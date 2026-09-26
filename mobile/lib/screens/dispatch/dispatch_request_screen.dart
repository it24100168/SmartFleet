import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/empty_widget_view.dart';
import '../../core/widgets/error_widget_view.dart';
import '../../core/widgets/loading_widget.dart';
import '../../models/dispatch_request_model.dart';
import '../../services/dispatch_service.dart';

class DispatchRequestScreen extends StatefulWidget {
  const DispatchRequestScreen({super.key});

  @override
  State<DispatchRequestScreen> createState() => _DispatchRequestScreenState();
}

class _DispatchRequestScreenState extends State<DispatchRequestScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final DispatchService _dispatchService = DispatchService();

  // Form State
  final _formKey = GlobalKey<FormState>();
  final _sourceZoneController = TextEditingController(text: 'WarehouseA-DockA1');
  final _destinationZoneController = TextEditingController(text: 'WarehouseA-DockB3');
  String _selectedCargo = 'Fragile';
  String _selectedPriority = 'High';
  DateTime _preferredTime = DateTime.now().add(const Duration(hours: 2));

  // GPS State
  double? _capturedLatitude;
  double? _capturedLongitude;
  bool _isLocating = false;
  String? _locationStatus;

  bool _isSubmitting = false;

  // History State
  List<DispatchRequestModel> _history = [];
  bool _isLoadingHistory = true;
  String? _historyError;

  final List<String> _cargoTypes = [
    'Fragile',
    'Standard',
    'Hazmat',
    'Refrigerated',
    'Heavy Machinery'
  ];

  final List<String> _priorities = ['Low', 'Medium', 'High', 'Critical'];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    _loadHistory();
  }

  @override
  void dispose() {
    _tabController.dispose();
    _sourceZoneController.dispose();
    _destinationZoneController.dispose();
    super.dispose();
  }

  Future<void> _loadHistory() async {
    setState(() {
      _isLoadingHistory = true;
      _historyError = null;
    });

    try {
      final list = await _dispatchService.fetchMyRequests();
      setState(() {
        _history = list;
        _isLoadingHistory = false;
      });
    } catch (e) {
      setState(() {
        _historyError = e.toString().replaceAll('Exception: ', '');
        _isLoadingHistory = false;
      });
    }
  }

  Future<void> _captureGpsLocation() async {
    setState(() {
      _isLocating = true;
      _locationStatus = 'Detecting current GPS location...';
    });

    try {
      bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) {
        setState(() {
          _isLocating = false;
          _locationStatus = 'Location services disabled on device.';
        });
        return;
      }

      LocationPermission permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
        if (permission == LocationPermission.denied) {
          setState(() {
            _isLocating = false;
            _locationStatus = 'Location permission denied by user.';
          });
          return;
        }
      }

      if (permission == LocationPermission.deniedForever) {
        setState(() {
          _isLocating = false;
          _locationStatus = 'Location permission permanently denied in settings.';
        });
        return;
      }

      final position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high,
        timeLimit: const Duration(seconds: 10),
      );

      setState(() {
        _capturedLatitude = position.latitude;
        _capturedLongitude = position.longitude;
        _isLocating = false;
        _locationStatus =
            'GPS Captured: ${position.latitude.toStringAsFixed(5)}, ${position.longitude.toStringAsFixed(5)}';
      });
    } catch (e) {
      setState(() {
        _isLocating = false;
        _locationStatus = 'Failed to acquire GPS fix ($e)';
      });
    }
  }

  Future<void> _submitRequest() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() {
      _isSubmitting = true;
    });

    try {
      final newOrder = await _dispatchService.createDispatchRequest(
        sourceZone: _sourceZoneController.text,
        destinationZone: _destinationZoneController.text,
        cargoType: _selectedCargo,
        priority: _selectedPriority,
        preferredTimeWindow: _preferredTime,
        latitude: _capturedLatitude,
        longitude: _capturedLongitude,
      );

      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Dispatch order #${newOrder.id.substring(0, 8)} created!'),
          backgroundColor: AppColors.success,
        ),
      );

      _loadHistory();
      _tabController.animateTo(1);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Error: ${e.toString().replaceAll("Exception: ", "")}'),
          backgroundColor: AppColors.danger,
        ),
      );
    } finally {
      if (mounted) {
        setState(() {
          _isSubmitting = false;
        });
      }
    }
  }

  Future<void> _triggerPlanGeneration(DispatchRequestModel item) async {
    try {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Generating mission plan for #${item.id.substring(0, 8)}...'),
          duration: const Duration(seconds: 2),
        ),
      );

      final plan = await _dispatchService.generatePlan(item.id);
      _loadHistory();

      if (!mounted) return;
      _showPlanBottomSheet(plan, item.id);
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Planning failed: ${e.toString().replaceAll("Exception: ", "")}'),
          backgroundColor: AppColors.danger,
        ),
      );
    }
  }

  void _showPlanBottomSheet(MissionPlanModel plan, String orderId) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: AppColors.surface,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) {
        return DraggableScrollableSheet(
          initialChildSize: 0.7,
          minChildSize: 0.4,
          maxChildSize: 0.95,
          expand: false,
          builder: (_, scrollController) {
            return Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Center(
                    child: Container(
                      width: 40,
                      height: 4,
                      decoration: BoxDecoration(
                        color: Colors.white24,
                        borderRadius: BorderRadius.circular(2),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),
                  Row(
                    children: [
                      const Icon(Icons.hub_outlined, color: AppColors.primaryLight, size: 24),
                      const SizedBox(width: 8),
                      Text(
                        'Mission Plan (#${orderId.substring(0, 8)})',
                        style: const TextStyle(
                          color: AppColors.textMain,
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    'Created: ${plan.createdAt}',
                    style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                  ),
                  const Divider(color: Colors.white12, height: 24),
                  Expanded(
                    child: ListView.separated(
                      controller: scrollController,
                      itemCount: plan.plan.length,
                      separatorBuilder: (_, __) => const SizedBox(height: 10),
                      itemBuilder: (_, i) {
                        final step = plan.plan[i];
                        return Container(
                          padding: const EdgeInsets.all(12),
                          decoration: BoxDecoration(
                            color: AppColors.card,
                            borderRadius: BorderRadius.circular(10),
                            border: Border.all(color: Colors.white10),
                          ),
                          child: Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              CircleAvatar(
                                radius: 14,
                                backgroundColor: AppColors.primary.withOpacity(0.25),
                                child: Text(
                                  '${step.stepNumber}',
                                  style: const TextStyle(
                                    color: AppColors.primaryLight,
                                    fontSize: 12,
                                    fontWeight: FontWeight.bold,
                                  ),
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      step.stepName,
                                      style: const TextStyle(
                                        color: AppColors.textMain,
                                        fontWeight: FontWeight.w600,
                                        fontSize: 14,
                                      ),
                                    ),
                                    if (step.assignedAgent != null) ...[
                                      const SizedBox(height: 4),
                                      Text(
                                        'Agent: ${step.assignedAgent}',
                                        style: TextStyle(
                                          color: AppColors.primaryLight.withOpacity(0.85),
                                          fontSize: 11,
                                        ),
                                      ),
                                    ],
                                  ],
                                ),
                              ),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                                decoration: BoxDecoration(
                                  color: AppColors.warning.withOpacity(0.15),
                                  borderRadius: BorderRadius.circular(12),
                                ),
                                child: Text(
                                  step.status,
                                  style: const TextStyle(
                                    color: AppColors.warning,
                                    fontSize: 11,
                                    fontWeight: FontWeight.w600,
                                  ),
                                ),
                              ),
                            ],
                          ),
                        );
                      },
                    ),
                  ),
                ],
              ),
            );
          },
        );
      },
    );
  }

  Color _getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'planned':
        return AppColors.primaryLight;
      case 'approved':
      case 'completed':
        return AppColors.success;
      case 'awaitingapproval':
        return Colors.purpleAccent;
      case 'intransit':
        return Colors.blueAccent;
      case 'rejected':
      case 'failed':
        return AppColors.danger;
      default:
        return AppColors.warning;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: AppColors.surface,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textMain),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: const Text(
          'Dispatch Missions',
          style: TextStyle(color: AppColors.textMain, fontWeight: FontWeight.bold),
        ),
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: AppColors.primaryLight,
          labelColor: AppColors.primaryLight,
          unselectedLabelColor: AppColors.textMuted,
          tabs: const [
            Tab(icon: Icon(Icons.add_circle_outline), text: 'New Request'),
            Tab(icon: Icon(Icons.history), text: 'My History'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildFormTab(),
          _buildHistoryTab(),
        ],
      ),
    );
  }

  Widget _buildFormTab() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Submit Cargo Transport Order',
              style: TextStyle(
                color: AppColors.textMain,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 4),
            const Text(
              'Enter pickup and dropoff warehouse docks. The Mission Planner Agent will schedule rovers automatically.',
              style: TextStyle(color: AppColors.textMuted, fontSize: 13),
            ),
            const SizedBox(height: 20),

            // Pickup Dock
            TextFormField(
              controller: _sourceZoneController,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                labelText: 'Source Zone (Pickup Dock)',
                labelStyle: const TextStyle(color: AppColors.textMuted),
                prefixIcon: const Icon(Icons.flight_takeoff, color: AppColors.primaryLight),
                filled: true,
                fillColor: AppColors.card,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              ),
              validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
            ),
            const SizedBox(height: 16),

            // Destination Dock
            TextFormField(
              controller: _destinationZoneController,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                labelText: 'Destination Zone (Dropoff Dock)',
                labelStyle: const TextStyle(color: AppColors.textMuted),
                prefixIcon: const Icon(Icons.flight_land, color: AppColors.primaryLight),
                filled: true,
                fillColor: AppColors.card,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              ),
              validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
            ),
            const SizedBox(height: 16),

            // Cargo Type Dropdown
            DropdownButtonFormField<String>(
              value: _selectedCargo,
              dropdownColor: AppColors.surface,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                labelText: 'Cargo Classification',
                labelStyle: const TextStyle(color: AppColors.textMuted),
                prefixIcon: const Icon(Icons.inventory_2_outlined, color: AppColors.primaryLight),
                filled: true,
                fillColor: AppColors.card,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              ),
              items: _cargoTypes
                  .map((c) => DropdownMenuItem(value: c, child: Text(c)))
                  .toList(),
              onChanged: (v) => setState(() => _selectedCargo = v!),
            ),
            const SizedBox(height: 16),

            // Priority Dropdown
            DropdownButtonFormField<String>(
              value: _selectedPriority,
              dropdownColor: AppColors.surface,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                labelText: 'Priority Level',
                labelStyle: const TextStyle(color: AppColors.textMuted),
                prefixIcon: const Icon(Icons.flag_outlined, color: AppColors.primaryLight),
                filled: true,
                fillColor: AppColors.card,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10)),
              ),
              items: _priorities
                  .map((p) => DropdownMenuItem(value: p, child: Text(p)))
                  .toList(),
              onChanged: (v) => setState(() => _selectedPriority = v!),
            ),
            const SizedBox(height: 20),

            // GPS Location Card
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppColors.card,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(
                  color: _capturedLatitude != null
                      ? AppColors.primaryLight.withOpacity(0.5)
                      : Colors.white12,
                ),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Icon(
                        Icons.my_location,
                        color: _capturedLatitude != null
                            ? AppColors.success
                            : AppColors.primaryLight,
                      ),
                      const SizedBox(width: 8),
                      const Text(
                        'Operator GPS Location',
                        style: TextStyle(
                          color: AppColors.textMain,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    _locationStatus ?? 'Attach your current coordinates to assist rover dispatch.',
                    style: TextStyle(
                      color: _capturedLatitude != null ? AppColors.success : AppColors.textMuted,
                      fontSize: 12,
                    ),
                  ),
                  const SizedBox(height: 12),
                  SizedBox(
                    width: double.infinity,
                    child: OutlinedButton.icon(
                      onPressed: _isLocating ? null : _captureGpsLocation,
                      icon: _isLocating
                          ? const SizedBox(
                              width: 14,
                              height: 14,
                              child: CircularProgressIndicator(strokeWidth: 2),
                            )
                          : const Icon(Icons.gps_fixed, size: 16),
                      label: Text(_capturedLatitude == null
                          ? 'Capture Current GPS'
                          : 'Update GPS Coordinates'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: AppColors.primaryLight,
                        side: const BorderSide(color: AppColors.primaryLight),
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Submit Button
            SizedBox(
              width: double.infinity,
              height: 50,
              child: ElevatedButton.icon(
                onPressed: _isSubmitting ? null : _submitRequest,
                icon: _isSubmitting
                    ? const SizedBox(
                        width: 18,
                        height: 18,
                        child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                      )
                    : const Icon(Icons.send),
                label: Text(
                  _isSubmitting ? 'Submitting Order...' : 'Submit Dispatch Request',
                  style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.primary,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHistoryTab() {
    if (_isLoadingHistory) {
      return const LoadingWidget(message: 'Loading your dispatch requests...');
    }

    if (_historyError != null) {
      return ErrorWidgetView(
        message: _historyError!,
        onRetry: _loadHistory,
      );
    }

    if (_history.isEmpty) {
      return EmptyWidgetView(
        icon: Icons.local_shipping_outlined,
        title: 'No Dispatch Requests',
        description: 'You have not submitted any dispatch requests yet. Create your first request.',
        actionLabel: 'Create Request',
        onAction: () => _tabController.animateTo(0),
      );
    }

    return RefreshIndicator(
      onRefresh: _loadHistory,
      color: AppColors.primaryLight,
      backgroundColor: AppColors.surface,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _history.length,
        separatorBuilder: (_, __) => const SizedBox(height: 12),
        itemBuilder: (_, i) {
          final item = _history[i];
          final statusColor = _getStatusColor(item.status);

          return Card(
            color: AppColors.card,
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
              side: const BorderSide(color: Colors.white10),
            ),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text(
                        '#${item.id.substring(0, 8)}',
                        style: const TextStyle(
                          fontFamily: 'monospace',
                          color: AppColors.primaryLight,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: statusColor.withOpacity(0.15),
                          borderRadius: BorderRadius.circular(12),
                          border: Border.all(color: statusColor.withOpacity(0.4)),
                        ),
                        child: Text(
                          item.status,
                          style: TextStyle(
                            color: statusColor,
                            fontSize: 11,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Row(
                    children: [
                      const Icon(Icons.arrow_forward_rounded, color: AppColors.primaryLight, size: 16),
                      const SizedBox(width: 6),
                      Expanded(
                        child: Text(
                          '${item.sourceZone} \u2192 ${item.destinationZone}',
                          style: const TextStyle(
                            color: AppColors.textMain,
                            fontWeight: FontWeight.w600,
                            fontSize: 14,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Text(
                        'Cargo: ${item.cargoType}',
                        style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                      ),
                      const Text(' \u2022 ', style: TextStyle(color: AppColors.textMuted)),
                      Text(
                        'Priority: ${item.priority}',
                        style: const TextStyle(color: AppColors.textMuted, fontSize: 12),
                      ),
                    ],
                  ),
                  if (item.latitude != null && item.longitude != null) ...[
                    const SizedBox(height: 4),
                    Row(
                      children: [
                        const Icon(Icons.location_on_outlined, size: 12, color: AppColors.textMuted),
                        const SizedBox(width: 4),
                        Text(
                          'GPS: ${item.latitude!.toStringAsFixed(4)}, ${item.longitude!.toStringAsFixed(4)}',
                          style: const TextStyle(color: AppColors.textMuted, fontSize: 11),
                        ),
                      ],
                    ),
                  ],
                  const SizedBox(height: 12),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.end,
                    children: [
                      if (item.status.toLowerCase() == 'pending') ...[
                        OutlinedButton.icon(
                          onPressed: () => _triggerPlanGeneration(item),
                          icon: const Icon(Icons.smart_toy_outlined, size: 16),
                          label: const Text('Generate Plan'),
                          style: OutlinedButton.styleFrom(
                            foregroundColor: AppColors.primaryLight,
                            side: const BorderSide(color: AppColors.primaryLight),
                          ),
                        ),
                      ],
                      if (item.latestPlan != null) ...[
                        const SizedBox(width: 8),
                        ElevatedButton.icon(
                          onPressed: () => _showPlanBottomSheet(item.latestPlan!, item.id),
                          icon: const Icon(Icons.checklist, size: 16),
                          label: const Text('View Plan'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.surface,
                            foregroundColor: AppColors.textMain,
                          ),
                        ),
                      ],
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }
}
