import 'package:flutter/foundation.dart';

class ApiConstants {
  static String get baseUrl {
    const configured = String.fromEnvironment('API_BASE_URL');
    if (configured.isNotEmpty) return configured;
    return !kIsWeb && defaultTargetPlatform == TargetPlatform.android
        ? 'http://10.0.2.2:5078/api' : 'http://localhost:5078/api';
  }
  // Endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String operatorTest = '/test/operator-only';
  static const String technicianTest = '/test/technician-only';
  static const String dispatchRequests = '/dispatch-requests';
  static const String breakdownReports = '/breakdown-reports';
}
