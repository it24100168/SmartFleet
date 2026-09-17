import 'package:flutter/material.dart';
import '../../core/constants/app_colors.dart';
import '../../core/widgets/empty_widget_view.dart';

class DispatchRequestScreen extends StatelessWidget {
  const DispatchRequestScreen({super.key});

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
          'Request Cargo Dispatch',
          style: TextStyle(color: AppColors.textMain, fontWeight: FontWeight.bold),
        ),
      ),
      body: const EmptyWidgetView(
        icon: Icons.local_shipping_outlined,
        title: 'Dispatch Pipeline Ready',
        description:
            'Dispatch request forms, cargo pickup/dropoff selector, and priority queuing will be implemented in Phase 2 for Factory Operators.',
      ),
    );
  }
}
