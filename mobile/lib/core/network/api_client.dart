import 'dart:convert';
import 'package:http/http.dart' as http;
import '../constants/api_constants.dart';
import '../storage/secure_storage_service.dart';

class ApiClient {
  final SecureStorageService _storageService;
  final http.Client _httpClient;

  ApiClient({
    SecureStorageService? storageService,
    http.Client? httpClient,
  })  : _storageService = storageService ?? SecureStorageService(),
        _httpClient = httpClient ?? http.Client();

  Future<Map<String, String>> _buildHeaders({bool requiresAuth = true}) async {
    final headers = <String, String>{
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };

    if (requiresAuth) {
      final token = await _storageService.getToken();
      if (token != null && token.isNotEmpty) {
        headers['Authorization'] = 'Bearer $token';
      }
    }

    return headers;
  }

  Future<http.Response> get(String endpoint, {bool requiresAuth = true}) async {
    final uri = Uri.parse('${ApiConstants.baseUrl}$endpoint');
    final headers = await _buildHeaders(requiresAuth: requiresAuth);

    return await _httpClient.get(uri, headers: headers);
  }

  Future<http.Response> post(
    String endpoint,
    Map<String, dynamic> body, {
    bool requiresAuth = true,
  }) async {
    final uri = Uri.parse('${ApiConstants.baseUrl}$endpoint');
    final headers = await _buildHeaders(requiresAuth: requiresAuth);

    return await _httpClient.post(
      uri,
      headers: headers,
      body: jsonEncode(body),
    );
  }
}
