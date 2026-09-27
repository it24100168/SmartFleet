import 'package:flutter/material.dart';
import '../../services/fleet_service.dart';

class FleetScreen extends StatefulWidget {
  final FleetService? service;
  final bool supervisor;
  const FleetScreen({super.key, this.service, this.supervisor = false});
  @override
  State<FleetScreen> createState() => _FleetScreenState();
}

class _FleetScreenState extends State<FleetScreen> {
  late final FleetService service = widget.service ?? FleetService();
  Map<String, dynamic>? fleet;
  String? error;
  String search = '', status = 'All';
  bool loading = true, busy = false;
  @override
  void initState() {
    super.initState();
    load();
  }

  Future<void> load() async {
    try {
      final next = await service.fetch();
      if (mounted) {
        setState(() {
          fleet = next;
          error = null;
          loading = false;
        });
      }
    } catch (_) {
      if (mounted) {
        setState(() {
          error = 'Fleet could not be refreshed. Check the API connection.';
          loading = false;
        });
      }
    }
  }

  Future<void> charge(String id) async {
    setState(() => busy = true);
    try {
      await service.charge(id);
      await load();
    } catch (e) {
      if (mounted) {
        setState(() => error = e.toString().replaceFirst('Exception: ', ''));
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final rovers =
        ((fleet?['rovers'] as List?) ?? []).cast<Map<String, dynamic>>();
    final visible = rovers
        .where((r) =>
            (status == 'All' || r['status'] == status) &&
            '${r['identifier']} ${r['locationZone']}'
                .toLowerCase()
                .contains(search.toLowerCase()))
        .toList()
      ..sort((a, b) =>
          (a['identifier'] as String).compareTo(b['identifier'] as String));
    return Scaffold(
      appBar: AppBar(title: const Text('Fleet telemetry'), actions: [
        IconButton(
            onPressed: busy ? null : load,
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh fleet')
      ]),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: load,
              child: ListView(
                  padding: const EdgeInsets.all(16),
                  physics: const AlwaysScrollableScrollPhysics(),
                  children: [
                    if (error != null)
                      Text(error!, style: const TextStyle(color: Colors.red)),
                    Text(
                        '${rovers.length} rovers • ${fleet?['databaseProvider'] ?? 'Database'}'),
                    const Text(
                        'Snapshot from the backend. Pull down to refresh.'),
                    TextField(
                        key: const Key('fleet-search'),
                        decoration: const InputDecoration(
                            labelText: 'Search robot or zone'),
                        onChanged: (value) => setState(() => search = value)),
                    DropdownButton<String>(
                        value: status,
                        isExpanded: true,
                        items: [
                          'All',
                          'Idle',
                          'Reserved',
                          'Dispatched',
                          'Charging',
                          'Maintenance',
                          'Faulted'
                        ]
                            .map((s) =>
                                DropdownMenuItem(value: s, child: Text(s)))
                            .toList(),
                        onChanged: (value) {
                          if (value != null) setState(() => status = value);
                        }),
                    if (visible.isEmpty)
                      const Padding(
                          padding: EdgeInsets.all(24),
                          child: Text('No matching rovers')),
                    ...visible.map((r) => Card(
                        child: Padding(
                            padding: const EdgeInsets.all(16),
                            child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(r['identifier'] as String,
                                      style: Theme.of(context)
                                          .textTheme
                                          .titleLarge),
                                  Text(
                                      '${r['status']} • ${r['batteryPercentage']}% battery'),
                                  Text(r['locationZone'] as String),
                                  if (r['currentMissionId'] != null)
                                    Text('Mission: ${r['currentMissionId']}'),
                                  if (widget.supervisor &&
                                      fleet?['demo'] == true &&
                                      r['currentMissionId'] == null &&
                                      ['Idle', 'Charging']
                                          .contains(r['status']))
                                    TextButton(
                                        onPressed: busy
                                            ? null
                                            : () => charge(r['id'] as String),
                                        child:
                                            const Text('Demo charge to 100%')),
                                ])))),
                  ])),
    );
  }
}
