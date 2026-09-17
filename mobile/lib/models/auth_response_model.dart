import 'role.dart';

class AuthResponseModel {
  final String token;
  final String id;
  final String name;
  final String email;
  final Role role;
  final DateTime? expiresAt;

  AuthResponseModel({
    required this.token,
    required this.id,
    required this.name,
    required this.email,
    required this.role,
    this.expiresAt,
  });

  factory AuthResponseModel.fromJson(Map<String, dynamic> json) {
    return AuthResponseModel(
      token: json['token'] as String,
      id: json['id'] as String,
      name: json['name'] as String? ?? '',
      email: json['email'] as String? ?? '',
      role: RoleExtension.fromString(json['role'] as String? ?? 'Operator'),
      expiresAt: json['expiresAt'] != null
          ? DateTime.tryParse(json['expiresAt'] as String)
          : null,
    );
  }
}
