class BreakdownReportModel {
  final String id;
  final String? roverId;
  final String reportedById;
  final String reportedByName;
  final String symptomCategory;
  final String description;
  final String? photoUrl;
  final String? errorCode;
  final String status;
  final String? likelyPart;
  final String? recommendedAction;
  final String? severity;
  final int? estimatedRepairHours;
  final DateTime createdAt;

  BreakdownReportModel({
    required this.id,
    this.roverId,
    required this.reportedById,
    required this.reportedByName,
    required this.symptomCategory,
    required this.description,
    this.photoUrl,
    this.errorCode,
    required this.status,
    this.likelyPart,
    this.recommendedAction,
    this.severity,
    this.estimatedRepairHours,
    required this.createdAt,
  });

  factory BreakdownReportModel.fromJson(Map<String, dynamic> json) {
    String? likelyPart;
    String? recommendedAction;
    String? severity;
    int? estimatedRepairHours;

    if (json['diagnosisResult'] != null && json['diagnosisResult'] is Map) {
      final diag = json['diagnosisResult'] as Map<String, dynamic>;
      likelyPart = diag['likelyPart']?.toString();
      recommendedAction = diag['recommendedAction']?.toString();
      severity = diag['severity']?.toString();
      estimatedRepairHours = diag['estimatedRepairHours'] is int
          ? diag['estimatedRepairHours'] as int
          : int.tryParse(diag['estimatedRepairHours']?.toString() ?? '');
    }

    return BreakdownReportModel(
      id: json['id']?.toString() ?? '',
      roverId: json['roverId']?.toString(),
      reportedById: json['reportedById']?.toString() ?? '',
      reportedByName: json['reportedByName']?.toString() ?? 'SmartFleet Operator',
      symptomCategory: json['symptomCategory']?.toString() ?? 'General',
      description: json['description']?.toString() ?? '',
      photoUrl: json['photoUrl']?.toString(),
      errorCode: json['errorCode']?.toString(),
      status: json['status']?.toString() ?? 'Reported',
      likelyPart: likelyPart,
      recommendedAction: recommendedAction,
      severity: severity,
      estimatedRepairHours: estimatedRepairHours,
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'].toString()) ?? DateTime.now()
          : DateTime.now(),
    );
  }
}
