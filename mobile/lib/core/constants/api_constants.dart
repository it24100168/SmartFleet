import 'package:flutter/foundation.dart';

class ApiConstants {
  // Automatically routes to localhost for Web/Desktop, or 10.0.2.2 for Android Emulator
  static String get baseUrl =>
      kIsWeb ? 'http://localhost:5000/api' : 'http://10.0.2.2:5000/api';

  // Endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String operatorTest = '/test/operator-only';
  static const String technicianTest = '/test/technician-only';
  static const String breakdownReports = '/breakdown-reports';
}
