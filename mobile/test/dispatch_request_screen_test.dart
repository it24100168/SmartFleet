import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartfleet_mobile/models/dispatch_request_model.dart';
import 'package:smartfleet_mobile/screens/dispatch/dispatch_request_screen.dart';
import 'package:smartfleet_mobile/services/dispatch_service.dart';

class FakeDispatch extends DispatchService {
  bool failZones = false;
  List<DispatchRequestModel> history = [];
  Map<String, dynamic>? submitted;
  @override
  Future<List<Map<String, dynamic>>> fetchZones() async {
    if (failZones) throw Exception('Zones unavailable');
    return [
      {'id': 'WarehouseA-DockA1', 'label': 'Receiving'},
      {'id': 'WarehouseA-DockB3', 'label': 'Dispatch'},
    ];
  }

  @override
  Future<List<DispatchRequestModel>> fetchMyRequests() async => history;
  @override
  Future<DispatchRequestModel> createDispatchRequest(
      {required String sourceZone,
      required String destinationZone,
      required String cargoType,
      required String priority,
      required DateTime preferredTimeWindow,
      double? latitude,
      double? longitude}) async {
    submitted = {
      'source': sourceZone,
      'destination': destinationZone,
      'priority': priority,
      'time': preferredTimeWindow
    };
    final item = DispatchRequestModel.fromJson({
      'id': '12345678-0000-0000-0000-000000000001',
      'sourceZone': sourceZone,
      'destinationZone': destinationZone,
      'priority': priority,
      'cargoType': cargoType,
      'status': 'AwaitingApproval'
    });
    history = [item];
    return item;
  }
}

void main() {
  testWidgets(
      'Critical request uses map zones and shows approval wait, then refreshed completion',
      (tester) async {
    tester.view.physicalSize = const Size(900, 1600);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.resetPhysicalSize);
    addTearDown(tester.view.resetDevicePixelRatio);
    final service = FakeDispatch();
    await tester
        .pumpWidget(MaterialApp(home: DispatchRequestScreen(service: service)));
    await tester.pumpAndSettle();
    expect(find.byType(TextFormField), findsNothing);
    expect(find.textContaining('IST'), findsWidgets);
    await tester.tap(find.byKey(const Key('dispatch-priority')));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Critical').last);
    await tester.pumpAndSettle();
    expect(find.textContaining('require Supervisor approval'), findsOneWidget);
    await tester.ensureVisible(find.text('Next available slot'));
    await tester.tap(find.text('Next available slot'));
    await tester.ensureVisible(find.byKey(const Key('submit-dispatch')));
    await tester.tap(find.byKey(const Key('submit-dispatch')));
    await tester.pumpAndSettle();
    expect(service.submitted?['priority'], 'Critical');
    expect(service.submitted?['source'], 'WarehouseA-DockA1');
    expect(service.submitted?['destination'], 'WarehouseA-DockB3');
    expect((service.submitted?['time'] as DateTime).isUtc, isTrue);
    expect(find.text('AwaitingApproval'), findsOneWidget);
    expect(find.textContaining('will not move'), findsOneWidget);
    service.history = [
      DispatchRequestModel.fromJson(
          {'id': service.history.single.id, 'status': 'Completed'})
    ];
    await tester.pump(const Duration(seconds: 3));
    await tester.pump();
    expect(find.text('Completed'), findsOneWidget);
    await tester.pumpWidget(const SizedBox.shrink());
  });

  testWidgets('Unavailable zone catalog blocks submission and can retry',
      (tester) async {
    final service = FakeDispatch()..failZones = true;
    await tester
        .pumpWidget(MaterialApp(home: DispatchRequestScreen(service: service)));
    await tester.pumpAndSettle();
    expect(find.text('Zones unavailable'), findsOneWidget);
    final button =
        tester.widget<ElevatedButton>(find.byKey(const Key('submit-dispatch')));
    expect(button.onPressed, isNull);
    service.failZones = false;
    await tester.tap(find.text('Retry zones'));
    await tester.pumpAndSettle();
    expect(find.text('Zones unavailable'), findsNothing);
    await tester.pumpWidget(const SizedBox.shrink());
  });
}
