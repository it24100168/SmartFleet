import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:smartfleet_mobile/core/network/api_client.dart';
import 'package:smartfleet_mobile/core/storage/secure_storage_service.dart';
import 'package:smartfleet_mobile/models/auth_response_model.dart';
import 'package:smartfleet_mobile/models/role.dart';
import 'package:smartfleet_mobile/models/user_model.dart';
import 'package:smartfleet_mobile/services/auth_service.dart';

class RegistrationApi extends ApiClient {
  String? endpoint;
  Map<String, dynamic>? body;
  bool? requiresAuth;
  late http.Response response;

  @override
  Future<http.Response> post(String endpoint, Map<String, dynamic> body,
      {bool requiresAuth = true}) async {
    this.endpoint = endpoint;
    this.body = body;
    this.requiresAuth = requiresAuth;
    return response;
  }
}

class MemorySession extends SecureStorageService {
  AuthResponseModel? saved;

  @override
  Future<UserModel?> getUser() async => null;

  @override
  Future<String?> getToken() async => null;

  @override
  Future<void> saveAuthSession(AuthResponseModel response) async {
    saved = response;
  }
}

void main() {
  test('registration requests only Operator and starts a secure session',
      () async {
    final api = RegistrationApi()
      ..response = http.Response(
          jsonEncode({
            'token': 'signed-token',
            'id': 'user-id',
            'name': 'New Operator',
            'email': 'new@example.com',
            'role': 'Operator',
          }),
          201);
    final storage = MemorySession();
    final auth = AuthService(apiClient: api, storageService: storage);

    expect(
        await auth.register(' New Operator ', ' NEW@example.com ', 'secret12'),
        isTrue);
    expect(api.endpoint, '/auth/register');
    expect(api.requiresAuth, isFalse);
    expect(api.body, {
      'name': 'New Operator',
      'email': 'NEW@example.com',
      'password': 'secret12',
      'role': 'Operator',
    });
    expect(auth.currentUser?.role, Role.operator);
    expect(storage.saved?.token, 'signed-token');
  });

  test('registration validation failure leaves no authenticated session',
      () async {
    final api = RegistrationApi()
      ..response = http.Response(
          jsonEncode({
            'errors': {
              'Email': ['Email is already registered']
            }
          }),
          400);
    final storage = MemorySession();
    final auth = AuthService(apiClient: api, storageService: storage);

    expect(await auth.register('New Operator', 'taken@example.com', 'secret12'),
        isFalse);
    expect(auth.isAuthenticated, isFalse);
    expect(auth.errorMessage, 'Email is already registered');
    expect(storage.saved, isNull);
  });
}
