enum Role {
  operator,
  technician,
  supervisor,
}

extension RoleExtension on Role {
  String get name {
    switch (this) {
      case Role.operator:
        return 'Operator';
      case Role.technician:
        return 'Technician';
      case Role.supervisor:
        return 'Supervisor';
    }
  }

  static Role fromString(String value) {
    switch (value.toLowerCase()) {
      case 'technician':
        return Role.technician;
      case 'supervisor':
        return Role.supervisor;
      case 'operator':
      default:
        return Role.operator;
    }
  }
}
