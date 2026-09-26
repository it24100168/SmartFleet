class PlanStepModel {
  final int stepNumber;
  final String stepName;
  final String status;
  final String? assignedAgent;

  PlanStepModel({
    required this.stepNumber,
    required this.stepName,
    required this.status,
    this.assignedAgent,
  });

  factory PlanStepModel.fromJson(Map<String, dynamic> json) {
    return PlanStepModel(
      stepNumber: json['stepNumber'] as int? ?? 0,
      stepName: json['stepName'] as String? ?? '',
      status: json['status'] as String? ?? 'Pending',
      assignedAgent: json['assignedAgent'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'stepNumber': stepNumber,
        'stepName': stepName,
        'status': status,
        if (assignedAgent != null) 'assignedAgent': assignedAgent,
      };
}

class MissionPlanModel {
  final String dispatchRequestId;
  final List<PlanStepModel> plan;
  final String createdAt;

  MissionPlanModel({
    required this.dispatchRequestId,
    required this.plan,
    required this.createdAt,
  });

  factory MissionPlanModel.fromJson(Map<String, dynamic> json) {
    var rawPlan = json['plan'] as List<dynamic>? ?? [];
    var steps = rawPlan.map((s) => PlanStepModel.fromJson(s as Map<String, dynamic>)).toList();

    return MissionPlanModel(
      dispatchRequestId: json['dispatchRequestId'] as String? ?? '',
      plan: steps,
      createdAt: json['createdAt'] as String? ?? '',
    );
  }

  Map<String, dynamic> toJson() => {
        'dispatchRequestId': dispatchRequestId,
        'plan': plan.map((p) => p.toJson()).toList(),
        'createdAt': createdAt,
      };
}

class DispatchRequestModel {
  final String id;
  final String operatorId;
  final String? operatorName;
  final String? roverId;
  final String sourceZone;
  final String destinationZone;
  final String cargoType;
  final String priority;
  final DateTime preferredTimeWindow;
  final String status;
  final double? latitude;
  final double? longitude;
  final DateTime createdAt;
  final DateTime updatedAt;
  final MissionPlanModel? latestPlan;

  DispatchRequestModel({
    required this.id,
    required this.operatorId,
    this.operatorName,
    this.roverId,
    required this.sourceZone,
    required this.destinationZone,
    required this.cargoType,
    required this.priority,
    required this.preferredTimeWindow,
    required this.status,
    this.latitude,
    this.longitude,
    required this.createdAt,
    required this.updatedAt,
    this.latestPlan,
  });

  factory DispatchRequestModel.fromJson(Map<String, dynamic> json) {
    return DispatchRequestModel(
      id: json['id'] as String? ?? '',
      operatorId: json['operatorId'] as String? ?? '',
      operatorName: json['operatorName'] as String?,
      roverId: json['roverId'] as String?,
      sourceZone: json['sourceZone'] as String? ?? '',
      destinationZone: json['destinationZone'] as String? ?? '',
      cargoType: json['cargoType'] as String? ?? '',
      priority: json['priority'] as String? ?? 'Medium',
      preferredTimeWindow: json['preferredTimeWindow'] != null
          ? DateTime.tryParse(json['preferredTimeWindow'] as String) ?? DateTime.now()
          : DateTime.now(),
      status: json['status'] as String? ?? 'Pending',
      latitude: (json['latitude'] as num?)?.toDouble(),
      longitude: (json['longitude'] as num?)?.toDouble(),
      createdAt: json['createdAt'] != null
          ? DateTime.tryParse(json['createdAt'] as String) ?? DateTime.now()
          : DateTime.now(),
      updatedAt: json['updatedAt'] != null
          ? DateTime.tryParse(json['updatedAt'] as String) ?? DateTime.now()
          : DateTime.now(),
      latestPlan: json['latestPlan'] != null
          ? MissionPlanModel.fromJson(json['latestPlan'] as Map<String, dynamic>)
          : null,
    );
  }
}
