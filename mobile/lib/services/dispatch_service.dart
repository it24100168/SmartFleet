import 'dart:convert';
import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../models/dispatch_request_model.dart';

class DispatchService {
  final ApiClient _apiClient;

  DispatchService({ApiClient? apiClient}) : _apiClient = apiClient ?? ApiClient();

  Future<List<DispatchRequestModel>> fetchMyRequests() async {
    final response = await _apiClient.get(
      '${ApiConstants.dispatchRequests}?pageSize=50',
      requiresAuth: true,
    );

    if (response.statusCode == 200) {
      final Map<String, dynamic> data = jsonDecode(response.body);
      final List<dynamic> items = data['items'] as List<dynamic>? ?? [];
      return items.map((e) => DispatchRequestModel.fromJson(e as Map<String, dynamic>)).toList();
    } else {
      final Map<String, dynamic> err = jsonDecode(response.body);
      throw Exception(err['message'] ?? 'Failed to load dispatch requests (${response.statusCode})');
    }
  }

  Future<DispatchRequestModel> createDispatchRequest({
    required String sourceZone,
    required String destinationZone,
    required String cargoType,
    required String priority,
    required DateTime preferredTimeWindow,
    double? latitude,
    double? longitude,
  }) async {
    final body = {
      'sourceZone': sourceZone.trim(),
      'destinationZone': destinationZone.trim(),
      'cargoType': cargoType.trim(),
      'priority': priority.trim(),
      'preferredTimeWindow': preferredTimeWindow.toUtc().toIso8601String(),
      if (latitude != null) 'latitude': latitude,
      if (longitude != null) 'longitude': longitude,
    };

    final response = await _apiClient.post(
      ApiConstants.dispatchRequests,
      body,
      requiresAuth: true,
    );

    if (response.statusCode == 201 || response.statusCode == 200) {
      final Map<String, dynamic> data = jsonDecode(response.body);
      return DispatchRequestModel.fromJson(data);
    } else {
      final Map<String, dynamic> err = jsonDecode(response.body);
      throw Exception(err['message'] ?? 'Failed to create dispatch request (${response.statusCode})');
    }
  }

  Future<MissionPlanModel> generatePlan(String dispatchRequestId) async {
    final response = await _apiClient.post(
      '${ApiConstants.dispatchRequests}/$dispatchRequestId/plan',
      {},
      requiresAuth: true,
    );

    if (response.statusCode == 200) {
      final Map<String, dynamic> data = jsonDecode(response.body);
      return MissionPlanModel.fromJson(data);
    } else {
      final Map<String, dynamic> err = jsonDecode(response.body);
      throw Exception(err['message'] ?? 'Failed to generate mission plan (${response.statusCode})');
    }
  }
}
