import 'package:flutter/foundation.dart';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../../core/constants/api_constants.dart';
import '../../core/constants/app_colors.dart';
import '../../services/breakdown_service.dart';

class BreakdownReportScreen extends StatefulWidget {
  const BreakdownReportScreen({super.key});

  @override
  State<BreakdownReportScreen> createState() => _BreakdownReportScreenState();
}

class _BreakdownReportScreenState extends State<BreakdownReportScreen>
    with SingleTickerProviderStateMixin {
  late final TabController _tabController;
  final _formKey = GlobalKey<FormState>();

  // Form Fields
  String _selectedCategory = 'MotorOverheating';
  final _descriptionController = TextEditingController();
  final _errorCodeController = TextEditingController();
  final _roverIdController = TextEditingController();

  XFile? _selectedPhoto;
  Uint8List? _photoBytes;
  final ImagePicker _picker = ImagePicker();

  final List<String> _categories = [
    'MotorOverheating',
    'WheelJam',
    'SensorFault',
    'BatteryDegradation',
    'DrivetrainFailure',
    'Other',
  ];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      Provider.of<BreakdownService>(context, listen: false).fetchReports();
    });
  }

  @override
  void dispose() {
    _tabController.dispose();
    _descriptionController.dispose();
    _errorCodeController.dispose();
    _roverIdController.dispose();
    super.dispose();
  }

  Future<void> _pickImage(ImageSource source) async {
    try {
      final photo = await _picker.pickImage(
        source: source,
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 85,
      );

      if (photo != null) {
        final bytes = await photo.readAsBytes();
        setState(() {
          _selectedPhoto = photo;
          _photoBytes = bytes;
        });
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('Failed to pick image: $e'),
            backgroundColor: AppColors.danger,
          ),
        );
      }
    }
  }

  void _removePhoto() {
    setState(() {
      _selectedPhoto = null;
      _photoBytes = null;
    });
  }

  Future<void> _handleSubmit() async {
    if (!_formKey.currentState!.validate()) return;

    final breakdownService = Provider.of<BreakdownService>(context, listen: false);

    final success = await breakdownService.submitReport(
      symptomCategory: _selectedCategory,
      description: _descriptionController.text.trim(),
      errorCode: _errorCodeController.text.trim().isNotEmpty
          ? _errorCodeController.text.trim()
          : null,
      roverId: _roverIdController.text.trim().isNotEmpty
          ? _roverIdController.text.trim()
          : null,
      photo: _selectedPhoto,
    );

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Breakdown incident reported successfully!'),
          backgroundColor: AppColors.success,
        ),
      );

      // Reset form fields
      _descriptionController.clear();
      _errorCodeController.clear();
      _roverIdController.clear();
      _removePhoto();

      // Switch to history tab to show the new report
      _tabController.animateTo(1);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(breakdownService.errorMessage ?? 'Submission failed.'),
          backgroundColor: AppColors.danger,
        ),
      );
    }
  }

  Color _getStatusColor(String status) {
    switch (status) {
      case 'Reported':
        return AppColors.danger;
      case 'Diagnosing':
        return AppColors.warning;
      case 'ScheduledForRepair':
        return AppColors.primaryLight;
      case 'Repaired':
        return AppColors.success;
      default:
        return AppColors.textMuted;
    }
  }

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
          'Breakdown Management',
          style: TextStyle(color: AppColors.textMain, fontWeight: FontWeight.bold),
        ),
        bottom: TabBar(
          controller: _tabController,
          indicatorColor: AppColors.primaryLight,
          labelColor: AppColors.primaryLight,
          unselectedLabelColor: AppColors.textMuted,
          tabs: const [
            Tab(icon: Icon(Icons.report_problem_outlined), text: 'New Incident'),
            Tab(icon: Icon(Icons.history), text: 'Incident History'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _buildReportFormTab(),
          _buildHistoryTab(),
        ],
      ),
    );
  }

  // -------------------------------------------------------------
  // Tab 1: New Incident Report Form
  // -------------------------------------------------------------
  Widget _buildReportFormTab() {
    final breakdownService = Provider.of<BreakdownService>(context);

    return SingleChildScrollView(
      padding: const EdgeInsets.all(20),
      child: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Header Banner
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.white12),
              ),
              child: Row(
                children: [
                  Container(
                    width: 44,
                    height: 44,
                    decoration: BoxDecoration(
                      color: AppColors.danger.withOpacity(0.15),
                      borderRadius: BorderRadius.circular(10),
                    ),
                    child: const Icon(Icons.warning_amber_rounded, color: AppColors.danger),
                  ),
                  const SizedBox(width: 14),
                  const Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'File Rover Breakdown',
                          style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: AppColors.textMain),
                        ),
                        SizedBox(height: 2),
                        Text(
                          'Triaged immediately by the Maintenance Mechanic Agent.',
                          style: TextStyle(fontSize: 12, color: AppColors.textMuted),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),

            // Symptom Category Dropdown
            const Text(
              'SYMPTOM CATEGORY',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textMuted, letterSpacing: 0.5),
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 14),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: Colors.white12),
              ),
              child: DropdownButtonHideUnderline(
                child: DropdownButton<String>(
                  value: _selectedCategory,
                  isExpanded: true,
                  dropdownColor: AppColors.surface,
                  icon: const Icon(Icons.arrow_drop_down, color: AppColors.primaryLight),
                  items: _categories.map((cat) {
                    return DropdownMenuItem<String>(
                      value: cat,
                      child: Text(cat, style: const TextStyle(color: AppColors.textMain)),
                    );
                  }).toList(),
                  onChanged: (val) {
                    if (val != null) {
                      setState(() => _selectedCategory = val);
                    }
                  },
                ),
              ),
            ),
            const SizedBox(height: 18),

            // Rover ID Field (Optional)
            const Text(
              'ROVER ID (OPTIONAL)',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textMuted, letterSpacing: 0.5),
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _roverIdController,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                hintText: 'e.g. RO-04',
                hintStyle: const TextStyle(color: Colors.white30),
                filled: true,
                fillColor: AppColors.surface,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Colors.white12)),
                enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Colors.white12)),
                focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: AppColors.primaryLight)),
                prefixIcon: const Icon(Icons.smart_toy_outlined, color: AppColors.textMuted),
              ),
            ),
            const SizedBox(height: 18),

            // Error Code Field (Optional)
            const Text(
              'ERROR CODE (OPTIONAL)',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textMuted, letterSpacing: 0.5),
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _errorCodeController,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                hintText: 'e.g. E204',
                hintStyle: const TextStyle(color: Colors.white30),
                filled: true,
                fillColor: AppColors.surface,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Colors.white12)),
                enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Colors.white12)),
                focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: AppColors.primaryLight)),
                prefixIcon: const Icon(Icons.code, color: AppColors.textMuted),
              ),
            ),
            const SizedBox(height: 18),

            // Detailed Description Field (Required)
            const Text(
              'ANOMALY DESCRIPTION *',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textMuted, letterSpacing: 0.5),
            ),
            const SizedBox(height: 8),
            TextFormField(
              controller: _descriptionController,
              maxLines: 4,
              style: const TextStyle(color: AppColors.textMain),
              decoration: InputDecoration(
                hintText: 'Describe unusual noises, overheating, wheel jams, or sensor faults...',
                hintStyle: const TextStyle(color: Colors.white30),
                filled: true,
                fillColor: AppColors.surface,
                border: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Colors.white12)),
                enabledBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: Colors.white12)),
                focusedBorder: OutlineInputBorder(borderRadius: BorderRadius.circular(10), borderSide: const BorderSide(color: AppColors.primaryLight)),
              ),
              validator: (value) {
                if (value == null || value.trim().isEmpty) {
                  return 'Please provide a detailed description of the incident.';
                }
                return null;
              },
            ),
            const SizedBox(height: 18),

            // Photo Capture Section
            const Text(
              'AUDIT PHOTO EVIDENCE',
              style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textMuted, letterSpacing: 0.5),
            ),
            const SizedBox(height: 8),
            if (_photoBytes != null)
              Container(
                margin: const EdgeInsets.only(bottom: 12),
                height: 180,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AppColors.primaryLight.withOpacity(0.5)),
                  image: DecorationImage(
                    image: MemoryImage(_photoBytes!),
                    fit: BoxFit.cover,
                  ),
                ),
                child: Stack(
                  children: [
                    Positioned(
                      top: 8,
                      right: 8,
                      child: GestureDetector(
                        onTap: _removePhoto,
                        child: Container(
                          padding: const EdgeInsets.all(6),
                          decoration: const BoxDecoration(
                            color: Colors.black54,
                            shape: BoxShape.circle,
                          ),
                          child: const Icon(Icons.close, color: Colors.white, size: 18),
                        ),
                      ),
                    ),
                  ],
                ),
              ),

            Row(
              children: [
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.camera_alt_outlined, size: 18),
                    label: const Text('Camera'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.textMain,
                      side: const BorderSide(color: Colors.white24),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                    onPressed: () => _pickImage(ImageSource.camera),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: OutlinedButton.icon(
                    icon: const Icon(Icons.photo_library_outlined, size: 18),
                    label: const Text('Gallery'),
                    style: OutlinedButton.styleFrom(
                      foregroundColor: AppColors.textMain,
                      side: const BorderSide(color: Colors.white24),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                    ),
                    onPressed: () => _pickImage(ImageSource.gallery),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 28),

            // Submit Button
            ElevatedButton(
              onPressed: breakdownService.isLoading ? null : _handleSubmit,
              style: ElevatedButton.styleFrom(
                backgroundColor: AppColors.primary,
                padding: const EdgeInsets.symmetric(vertical: 15),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
              ),
              child: breakdownService.isLoading
                  ? const SizedBox(
                      height: 20,
                      width: 20,
                      child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2),
                    )
                  : const Text(
                      'Submit Breakdown Report',
                      style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold, color: Colors.white),
                    ),
            ),
          ],
        ),
      ),
    );
  }

  // -------------------------------------------------------------
  // Tab 2: Incident History List
  // -------------------------------------------------------------
  Widget _buildHistoryTab() {
    final breakdownService = Provider.of<BreakdownService>(context);

    if (breakdownService.isLoading && breakdownService.reports.isEmpty) {
      return const Center(
        child: CircularProgressIndicator(color: AppColors.primaryLight),
      );
    }

    if (breakdownService.reports.isEmpty) {
      return RefreshIndicator(
        onRefresh: () => breakdownService.fetchReports(),
        color: AppColors.primaryLight,
        child: ListView(
          padding: const EdgeInsets.all(24),
          children: const [
            SizedBox(height: 60),
            Icon(Icons.check_circle_outline, size: 54, color: AppColors.success),
            SizedBox(height: 16),
            Text(
              'No Breakdown Reports',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.textMain),
            ),
            SizedBox(height: 8),
            Text(
              'All rovers are operating within normal parameters. New breakdown reports will appear here.',
              textAlign: TextAlign.center,
              style: TextStyle(color: AppColors.textMuted, fontSize: 13),
            ),
          ],
        ),
      );
    }

    return RefreshIndicator(
      onRefresh: () => breakdownService.fetchReports(),
      color: AppColors.primaryLight,
      child: ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: breakdownService.reports.length,
        separatorBuilder: (_, __) => const SizedBox(height: 12),
        itemBuilder: (context, index) {
          final report = breakdownService.reports[index];
          final statusColor = _getStatusColor(report.status);

          // Resolve full photo URL
          String? fullPhotoUrl;
          if (report.photoUrl != null && report.photoUrl!.isNotEmpty) {
            fullPhotoUrl = report.photoUrl!.startsWith('http')
                ? report.photoUrl
                : '${ApiConstants.baseUrl.replaceAll('/api', '')}${report.photoUrl}';
          }

          return Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(12),
              border: Border.all(color: Colors.white10),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Top Row: Status Chip & Date
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: statusColor.withOpacity(0.15),
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: statusColor.withOpacity(0.4)),
                      ),
                      child: Text(
                        report.status,
                        style: TextStyle(color: statusColor, fontWeight: FontWeight.bold, fontSize: 11),
                      ),
                    ),
                    Text(
                      '${report.createdAt.month}/${report.createdAt.day} ${report.createdAt.hour}:${report.createdAt.minute.toString().padLeft(2, '0')}',
                      style: const TextStyle(color: AppColors.textMuted, fontSize: 11),
                    ),
                  ],
                ),
                const SizedBox(height: 10),

                // Symptom Category & Error Code
                Row(
                  children: [
                    Text(
                      report.symptomCategory,
                      style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold, color: AppColors.textMain),
                    ),
                    if (report.errorCode != null) ...[
                      const SizedBox(width: 8),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: AppColors.warning.withOpacity(0.15),
                          borderRadius: BorderRadius.circular(4),
                        ),
                        child: Text(
                          report.errorCode!,
                          style: const TextStyle(color: AppColors.warning, fontSize: 11, fontWeight: FontWeight.w600),
                        ),
                      ),
                    ],
                  ],
                ),
                const SizedBox(height: 6),

                // Description
                Text(
                  report.description,
                  style: const TextStyle(color: Colors.white70, fontSize: 13),
                ),
                const SizedBox(height: 10),

                // Photo Evidence Thumbnail (if available)
                if (fullPhotoUrl != null) ...[
                  ClipRRect(
                    borderRadius: BorderRadius.circular(8),
                    child: Image.network(
                      fullPhotoUrl,
                      height: 120,
                      width: double.infinity,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                    ),
                  ),
                  const SizedBox(height: 10),
                ],

                // AI Agent Diagnostic Output Card
                if (report.likelyPart != null)
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.card,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppColors.primaryLight.withOpacity(0.3)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Row(
                          children: [
                            Icon(Icons.smart_toy, size: 14, color: AppColors.primaryLight),
                            SizedBox(width: 6),
                            Text(
                              'Maintenance Mechanic Agent Diagnosis',
                              style: TextStyle(color: AppColors.primaryLight, fontSize: 11, fontWeight: FontWeight.bold),
                            ),
                          ],
                        ),
                        const SizedBox(height: 6),
                        Text(
                          'Part: ${report.likelyPart}',
                          style: const TextStyle(color: AppColors.textMain, fontSize: 12, fontWeight: FontWeight.w600),
                        ),
                        Row(
                          children: [
                            Text(
                              'Action: ${report.recommendedAction ?? "ScheduleRepair"}',
                              style: const TextStyle(color: AppColors.success, fontSize: 11),
                            ),
                            if (report.estimatedRepairHours != null) ...[
                              const Text(' • ', style: TextStyle(color: AppColors.textMuted)),
                              Text(
                                'Est. ${report.estimatedRepairHours}h',
                                style: const TextStyle(color: AppColors.warning, fontSize: 11),
                              ),
                            ],
                          ],
                        ),
                      ],
                    ),
                  ),

                // Reporter Footer
                const SizedBox(height: 6),
                Text(
                  'Reported by: ${report.reportedByName}',
                  style: const TextStyle(color: AppColors.textMuted, fontSize: 11),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
