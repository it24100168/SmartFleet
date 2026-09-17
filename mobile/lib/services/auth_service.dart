import 'dart:convert';
import 'package:flutter/foundation.dart';
import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_storage_service.dart';
import '../models/auth_response_model.dart';
import '../models/user_model.dart';

class AuthService extends ChangeNotifier {
  final ApiClient _apiClient;
  final SecureStorageService _storageService;

  UserModel? _currentUser;
  bool _isLoading = true;
  String? _errorMessage;

  UserModel? get currentUser => _currentUser;
  bool get isAuthenticated => _currentUser != null;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  AuthService({
    ApiClient? apiClient,
    SecureStorageService? storageService,
  })  : _apiClient = apiClient ?? ApiClient(),
        _storageService = storageService ?? SecureStorageService() {
    _initSession();
  }

  Future<void> _initSession() async {
    _isLoading = true;
    notifyListeners();

    try {
      final user = await _storageService.getUser();
      final token = await _storageService.getToken();

      if (user != null && token != null) {
        _currentUser = user;
      }
    } catch (e) {
      debugPrint('Failed to restore auth session: $e');
      await _storageService.clearAuthSession();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> login(String email, String password) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.post(
        ApiConstants.login,
        {
          'email': email.trim(),
          'password': password,
        },
        requiresAuth: false,
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        final authResponse = AuthResponseModel.fromJson(data);

        await _storageService.saveAuthSession(authResponse);

        _currentUser = UserModel(
          id: authResponse.id,
          name: authResponse.name,
          email: authResponse.email,
          role: authResponse.role,
        );

        _isLoading = false;
        notifyListeners();
        return true;
      } else {
        final data = jsonDecode(response.body);
        _errorMessage = data['message'] ?? 'Authentication failed (${response.statusCode})';
        _isLoading = false;
        notifyListeners();
        return false;
      }
    } catch (e) {
      _errorMessage = 'Network connection error: $e';
      _isLoading = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> logout() async {
    await _storageService.clearAuthSession();
    _currentUser = null;
    notifyListeners();
  }
}
