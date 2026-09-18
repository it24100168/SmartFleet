class ApiConstants {
  // Update this to match your local IP or emulator bridge
  // Android Emulator: 'http://10.0.2.2:5000/api'
  // iOS Simulator / Desktop: 'http://localhost:5000/api'
  static const String baseUrl = 'http://10.0.2.2:5000/api';

  // Endpoints
  static const String login = '/auth/login';
  static const String register = '/auth/register';
  static const String operatorTest = '/test/operator-only';
  static const String technicianTest = '/test/technician-only';
  static const String dispatchRequests = '/dispatch-requests';
}
