import 'package:flutter_test/flutter_test.dart';
import 'package:smartfleet_mobile/models/dispatch_request_model.dart';

void main() {
  test('Dispatch history preserves backend workflow statuses', () {
    final mission = DispatchRequestModel.fromJson({
      'id': 'request-1', 'status': 'AwaitingApproval', 'sourceZone': 'WarehouseA-DockA1',
      'destinationZone': 'WarehouseA-DockB3', 'roverId': 'rover-uuid',
    });
    expect(mission.status, 'AwaitingApproval');
    expect(mission.roverId, 'rover-uuid');
  });
}
