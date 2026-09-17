import 'package:flutter/material.dart';
import '../constants/app_colors.dart';

class ErrorWidgetView extends StatelessWidget {
  final String message;
  final VoidCallback? onRetry;

  const ErrorWidgetView({
    super.key,
    required this.message,
    this.onRetry,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.danger.withOpacity(0.12),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.danger.withOpacity(0.3)),
      ),
      child: Row(
        children: [
          const Icon(Icons.error_outline, color: AppColors.danger),
          const SizedBox(width: 12),
          Expanded(
            child: Text(
              message,
              style: const TextStyle(color: Color(0xFFFCA5A5), fontSize: 13),
            ),
          ),
          if (onRetry != null)
            IconButton(
              icon: const Icon(Icons.refresh, color: AppColors.primaryLight, size: 20),
              onPressed: onRetry,
            ),
        ],
      ),
    );
  }
}
