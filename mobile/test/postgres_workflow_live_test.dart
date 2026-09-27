// Opt-in real HTTP contract test. This is not native-device or React UI evidence.
// flutter test --dart-define=LIVE_POSTGRES=true --dart-define=API_BASE_URL=http://localhost:5078/api test/postgres_workflow_live_test.dart
import 'dart:convert';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:smartfleet_mobile/core/constants/api_constants.dart';
import 'package:smartfleet_mobile/core/network/api_client.dart';
import 'package:smartfleet_mobile/core/storage/secure_storage_service.dart';
import 'package:smartfleet_mobile/services/dispatch_service.dart';

class MemorySession extends SecureStorageService {
  final String token;
  MemorySession(this.token);
  @override
  Future<String?> getToken() async => token;
}

void main() {
  test(
      'Flutter service submits Critical; other authenticated client approves; Flutter sees completion',
      () async {
    final base = ApiConstants.baseUrl;
    expect(Uri.parse(base).host, anyOf('localhost', '127.0.0.1'));
    final client = http.Client();
    addTearDown(client.close);
    final health =
        jsonDecode((await client.get(Uri.parse('$base/health'))).body);
    expect(health['demo'], true);
    expect(health['databaseProvider'], 'PostgreSQL');
    Future<String> login(String role) async {
      final response = await client.post(Uri.parse('$base/auth/login'),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({
            'email': '$role@demo.smartfleet',
            'password': 'DemoFleet!2026'
          }));
      expect(response.statusCode, 200);
      return jsonDecode(response.body)['token'] as String;
    }

    final operatorToken = await login('operator'),
        supervisorToken = await login('supervisor');
    final service = DispatchService(
        apiClient: ApiClient(
            httpClient: client, storageService: MemorySession(operatorToken)));
    final supervisorHeaders = {
      'Authorization': 'Bearer $supervisorToken',
      'Content-Type': 'application/json'
    };
    final zones = await service.fetchZones();
    expect(zones.length, greaterThanOrEqualTo(2));
    final request = await service.createDispatchRequest(
        sourceZone: 'WarehouseA-DockA1',
        destinationZone: 'WarehouseA-DockB3',
        cargoType: 'Standard',
        priority: 'Critical',
        preferredTimeWindow: DateTime.now().toUtc());
    Map<String, dynamic>? run;
    for (var i = 0; i < 30; i++) {
      final fleet = jsonDecode((await client.get(
              Uri.parse('$base/workflows/fleet'),
              headers: supervisorHeaders))
          .body);
      final matches = (fleet['runs'] as List)
          .where((r) => r['dispatchRequestId'] == request.id);
      if (matches.isNotEmpty) {
        run = Map<String, dynamic>.from(matches.first);
        if (run['status'] == 'AwaitingApproval') break;
      }
      await Future<void>.delayed(const Duration(seconds: 1));
    }
    expect(run?['status'], 'AwaitingApproval');
    expect(run?['progress'], 0);
    expect(
        (await service.fetchMyRequests())
            .singleWhere((r) => r.id == request.id)
            .status,
        'AwaitingApproval');
    final detail = jsonDecode((await client.get(
            Uri.parse('$base/workflows/${run!['id']}'),
            headers: supervisorHeaders))
        .body);
    final approvalId = detail['approval']['id'];
    final denied = await client.post(
        Uri.parse('$base/approval-requests/$approvalId/approve'),
        headers: {
          'Authorization': 'Bearer $operatorToken',
          'Content-Type': 'application/json'
        },
        body: '{}');
    expect(denied.statusCode, 403);
    final approved = await client.post(
        Uri.parse('$base/approval-requests/$approvalId/approve'),
        headers: supervisorHeaders,
        body:
            jsonEncode({'reviewNotes': 'Live two-session integration check'}));
    expect(approved.statusCode, 200);
    var status = '';
    for (var i = 0; i < 60; i++) {
      status = (await service.fetchMyRequests())
          .singleWhere((r) => r.id == request.id)
          .status;
      if (status == 'Completed' || status == 'Failed') break;
      await Future<void>.delayed(const Duration(seconds: 1));
    }
    expect(status, 'Completed');
  },
      skip: !const bool.fromEnvironment('LIVE_POSTGRES'),
      timeout: const Timeout(Duration(minutes: 2)));
}
