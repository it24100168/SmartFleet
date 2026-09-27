import 'dart:convert';
import '../core/network/api_client.dart';

class FleetService {
  final ApiClient client;
  FleetService({ApiClient? client}) : client = client ?? ApiClient();
  Future<Map<String, dynamic>> fetch() async {
    final response = await client.get('/workflows/fleet');
    if (response.statusCode != 200) {
      throw Exception('Could not load fleet (${response.statusCode}).');
    }
    return jsonDecode(response.body) as Map<String, dynamic>;
  }

  Future<void> charge(String id) async {
    final response = await client.post('/workflows/demo-rovers/$id/charge', {});
    if (response.statusCode != 200) {
      throw Exception(
          'Charge refused. Refresh the fleet and check rover eligibility.');
    }
  }
}
