import 'dart:convert';
import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;
import 'package:image_picker/image_picker.dart';
import '../core/constants/api_constants.dart';
import '../core/network/api_client.dart';
import '../core/storage/secure_storage_service.dart';
import '../models/breakdown_report_model.dart';

class BreakdownService extends ChangeNotifier {
  final ApiClient _apiClient;
  final SecureStorageService _storageService;

  List<BreakdownReportModel> _reports = [];
  bool _isLoading = false;
  String? _errorMessage;

  List<BreakdownReportModel> get reports => _reports;
  bool get isLoading => _isLoading;
  String? get errorMessage => _errorMessage;

  BreakdownService({
    ApiClient? apiClient,
    SecureStorageService? storageService,
  })  : _apiClient = apiClient ?? ApiClient(),
        _storageService = storageService ?? SecureStorageService();

  Future<void> fetchReports() async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final response = await _apiClient.get(ApiConstants.breakdownReports);

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body) as Map<String, dynamic>;
        final items = data['items'] as List<dynamic>? ?? [];
        _reports = items
            .map((item) => BreakdownReportModel.fromJson(item as Map<String, dynamic>))
            .toList();
      } else {
        _errorMessage = 'Failed to load breakdown reports (${response.statusCode})';
      }
    } catch (e) {
      _errorMessage = 'Network error: $e';
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<bool> submitReport({
    required String symptomCategory,
    required String description,
    String? errorCode,
    String? roverId,
    XFile? photo,
  }) async {
    _isLoading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final uri = Uri.parse('${ApiConstants.baseUrl}${ApiConstants.breakdownReports}');
      final request = http.MultipartRequest('POST', uri);

      // Attach auth bearer token
      final token = await _storageService.getToken();
      if (token != null && token.isNotEmpty) {
        request.headers['Authorization'] = 'Bearer $token';
      }
      request.headers['Accept'] = 'application/json';

      // Add form fields
      request.fields['symptomCategory'] = symptomCategory.trim();
      request.fields['description'] = description.trim();
      if (errorCode != null && errorCode.trim().isNotEmpty) {
        request.fields['errorCode'] = errorCode.trim();
      }
      if (roverId != null && roverId.trim().isNotEmpty) {
        request.fields['roverId'] = roverId.trim();
      }

      // Add photo file via byte stream (works on Web, Android, iOS, Desktop)
      if (photo != null) {
        final bytes = await photo.readAsBytes();
        final multipartFile = http.MultipartFile.fromBytes(
          'photo',
          bytes,
          filename: photo.name,
        );
        request.files.add(multipartFile);
      }

      final streamedResponse = await request.send();
      final response = await http.Response.fromStream(streamedResponse);

      if (response.statusCode == 201 || response.statusCode == 200) {
        // Refresh list
        await fetchReports();
        return true;
      } else {
        try {
          final errBody = jsonDecode(response.body);
          _errorMessage = errBody['message'] ?? 'Failed to submit report (${response.statusCode})';
        } catch (_) {
          _errorMessage = 'Failed to submit report (${response.statusCode})';
        }
        return false;
      }
    } catch (e) {
      _errorMessage = 'Network connection error: $e';
      return false;
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }
}
