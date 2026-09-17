import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/empty_widget_view.dart';

class BreakdownReportScreen extends StatelessWidget {
  const BreakdownReportScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.background,
      appBar: AppBar(
        backgroundColor: AppColors.surface,
        elevation: 0,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back, color: AppColors.textMain),
          onPressed: () => Navigator.of(context).pop(),
        ),
        title: const Text(
          'Report Breakdown Incident',
          style: TextStyle(color: AppColors.textMain, fontWeight: FontWeight.bold),
        ),
      ),
      body: const EmptyWidgetView(
        icon: Icons.warning_amber_rounded,
        title: 'Breakdown Reporter Ready',
        description:
            'Rover barcode scanning, fault symptom reporting, and emergency stopping triggers will be integrated here for Operators and Technicians.',
      ),
    );
  }
}
