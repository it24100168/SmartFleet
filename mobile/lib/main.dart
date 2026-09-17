import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'core/constants/app_colors.dart';
import 'routes/app_router.dart';
import 'services/auth_service.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(const SmartFleetApp());
}

class SmartFleetApp extends StatefulWidget {
  const SmartFleetApp({super.key});

  @override
  State<SmartFleetApp> createState() => _SmartFleetAppState();
}

class _SmartFleetAppState extends State<SmartFleetApp> {
  late final AuthService _authService;

  @override
  void initState() {
    super.initState();
    _authService = AuthService();
  }

  @override
  Widget build(BuildContext context) {
    final router = createRouter(_authService);

    return ChangeNotifierProvider<AuthService>.value(
      value: _authService,
      child: MaterialApp.router(
        title: 'SmartFleet',
        debugShowCheckedModeBanner: false,
        theme: ThemeData(
          brightness: Brightness.dark,
          scaffoldBackgroundColor: AppColors.background,
          primaryColor: AppColors.primary,
          colorScheme: const ColorScheme.dark(
            primary: AppColors.primary,
            secondary: AppColors.primaryLight,
            surface: AppColors.surface,
            background: AppColors.background,
          ),
          appBarTheme: const AppBarTheme(
            backgroundColor: AppColors.surface,
            elevation: 0,
            iconTheme: IconThemeData(color: AppColors.textMain),
            titleTextStyle: TextStyle(
              color: AppColors.textMain,
              fontSize: 18,
              fontWeight: FontWeight.bold,
            ),
          ),
        ),
        routerConfig: router,
      ),
    );
  }
}
