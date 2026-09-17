import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../../models/auth_response_model.dart';
import '../../models/role.dart';
import '../../models/user_model.dart';

class SecureStorageService {
  final FlutterSecureStorage _storage;

  SecureStorageService({FlutterSecureStorage? storage})
      : _storage = storage ?? const FlutterSecureStorage();

  static const String _keyToken = 'smartfleet_jwt_token';
  static const String _keyUserId = 'smartfleet_user_id';
  static const String _keyUserName = 'smartfleet_user_name';
  static const String _keyUserEmail = 'smartfleet_user_email';
  static const String _keyUserRole = 'smartfleet_user_role';

  Future<void> saveAuthSession(AuthResponseModel authResponse) async {
    await _storage.write(key: _keyToken, value: authResponse.token);
    await _storage.write(key: _keyUserId, value: authResponse.id);
    await _storage.write(key: _keyUserName, value: authResponse.name);
    await _storage.write(key: _keyUserEmail, value: authResponse.email);
    await _storage.write(key: _keyUserRole, value: authResponse.role.name);
  }

  Future<String?> getToken() async {
    return await _storage.read(key: _keyToken);
  }

  Future<UserModel?> getUser() async {
    final id = await _storage.read(key: _keyUserId);
    final name = await _storage.read(key: _keyUserName);
    final email = await _storage.read(key: _keyUserEmail);
    final roleString = await _storage.read(key: _keyUserRole);

    if (id == null || email == null || roleString == null) {
      return null;
    }

    final role = RoleExtension.fromString(roleString);

    return UserModel(
      id: id,
      name: name ?? '',
      email: email,
      role: role,
    );
  }

  Future<void> clearAuthSession() async {
    await _storage.delete(key: _keyToken);
    await _storage.delete(key: _keyUserId);
    await _storage.delete(key: _keyUserName);
    await _storage.delete(key: _keyUserEmail);
    await _storage.delete(key: _keyUserRole);
  }
}
