import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../screens/breakdown/breakdown_report_screen.dart';
import '../screens/dispatch/dispatch_request_screen.dart';
import '../screens/home/home_screen.dart';
import '../screens/login/login_screen.dart';
import '../services/auth_service.dart';

GoRouter createRouter(AuthService authService) {
  return GoRouter(
    initialLocation: '/',
    refreshListenable: authService,
    redirect: (BuildContext context, GoRouterState state) {
      final isLoggingIn = state.matchedLocation == '/login';
      final isAuthenticated = authService.isAuthenticated;

      // While initial session is loading, remain where we are
      if (authService.isLoading) return null;

      // If user is not authenticated and trying to access private screens
      if (!isAuthenticated && !isLoggingIn) {
        return '/login';
      }

      // If user is authenticated and navigating to login, redirect to home
      if (isAuthenticated && isLoggingIn) {
        return '/';
      }

      return null;
    },
    routes: [
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/',
        builder: (context, state) => const HomeScreen(),
      ),
      GoRoute(
        path: '/dispatch',
        builder: (context, state) => const DispatchRequestScreen(),
      ),
      GoRoute(
        path: '/breakdown',
        builder: (context, state) => const BreakdownReportScreen(),
      ),
    ],
  );
}
