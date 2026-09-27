import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartfleet_mobile/screens/fleet/fleet_screen.dart';
import 'package:smartfleet_mobile/services/fleet_service.dart';

class FakeFleet extends FleetService {
  bool fail = false;
  int charges = 0;
  @override
  Future<Map<String, dynamic>> fetch() async {
    if (fail) throw Exception('offline');
    return {
      'demo': true,
      'databaseProvider': 'PostgreSQL',
      'rovers': [
        {
          'id': 'one',
          'identifier': 'RO-01',
          'status': 'Idle',
          'batteryPercentage': charges > 0 ? 100 : 50,
          'locationZone': 'Dock A',
          'currentMissionId': null
        },
        {
          'id': 'two',
          'identifier': 'RO-02',
          'status': 'Reserved',
          'batteryPercentage': 80,
          'locationZone': 'Dock B',
          'currentMissionId': 'mission'
        }
      ]
    };
  }

  @override
  Future<void> charge(String id) async {
    charges++;
  }
}

void main() {
  testWidgets('Fleet search works and operators do not receive charge controls',
      (tester) async {
    await tester
        .pumpWidget(MaterialApp(home: FleetScreen(service: FakeFleet())));
    await tester.pumpAndSettle();
    expect(find.text('RO-01'), findsOneWidget);
    expect(find.text('RO-02'), findsOneWidget);
    expect(find.text('Demo charge to 100%'), findsNothing);
    await tester.enterText(find.byKey(const Key('fleet-search')), 'RO-02');
    await tester.pump();
    expect(find.text('RO-01'), findsNothing);
    expect(find.descendant(of: find.byType(Card), matching: find.text('RO-02')),
        findsOneWidget);
  });
  testWidgets('Supervisor charges only eligible rover and refreshes battery',
      (tester) async {
    final service = FakeFleet();
    await tester.pumpWidget(
        MaterialApp(home: FleetScreen(service: service, supervisor: true)));
    await tester.pumpAndSettle();
    expect(find.text('Demo charge to 100%'), findsOneWidget);
    await tester.tap(find.text('Demo charge to 100%'));
    await tester.pumpAndSettle();
    expect(service.charges, 1);
    expect(find.text('Idle • 100% battery'), findsOneWidget);
  });
  testWidgets('Failed load shows error and refresh recovers', (tester) async {
    final service = FakeFleet()..fail = true;
    await tester.pumpWidget(MaterialApp(home: FleetScreen(service: service)));
    await tester.pumpAndSettle();
    expect(find.textContaining('could not be refreshed'), findsOneWidget);
    service.fail = false;
    await tester.tap(find.byTooltip('Refresh fleet'));
    await tester.pumpAndSettle();
    expect(find.text('RO-01'), findsOneWidget);
  });
}
