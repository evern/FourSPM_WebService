# JSON Deserialization Troubleshooting Guide

## Common JSON Deserialization Issues in ASP.NET Core

This document provides guidance on troubleshooting JSON deserialization issues in ASP.NET Core Web API controllers, similar to what we encountered with the `ProgressController.AddOrUpdateExisting` endpoint.

### Issue: Model Binding Failures

When a client sends JSON data to an API endpoint with `[FromBody]` parameter, ASP.NET Core attempts to deserialize that JSON into the specified model type. Common issues include:

1. **Type Mismatches**: JSON property types don't match C# property types
2. **Required Fields**: Required properties are missing or null
3. **Case Sensitivity**: JSON property names don't match C# property names
4. **Empty Strings vs. NULL**: Empty strings can't be converted to non-string types

### Example Problems We Encountered

#### Problem 1: Empty String to GUID Conversion

```json
{
  "guid": "",  // Empty string can't be converted to System.Guid
  "deliverableGuid": "293eae36-6b20-4756-abf9-9181ac36adcf",
  "period": 3
}
```

Fixed by generating a valid GUID on the client side using `uuidv4()`.

#### Problem 2: Client Sending Audit Fields

```json
{
  "guid": "598b5e97-fb53-40d4-99e7-7c910a0032da",
  "deliverableGuid": "293eae36-6b20-4756-abf9-9181ac36adcf",
  "createdBy": "",  // Empty string can't be converted to System.Guid
  "updatedBy": ""
}
```

Fixed by removing audit fields from the client payload and letting the server handle them.

### Troubleshooting Steps

1. **Log Raw JSON**: Add code to capture and log the raw JSON received:

   ```csharp
   [HttpPost("endpoint")]
   public async Task<IActionResult> MyEndpoint([FromBody] System.Text.Json.JsonElement jsonElement)
   {
       string jsonString = jsonElement.GetRawText();
       _logger?.LogInformation($"Received raw JSON: {jsonString}");
       
       // Manual deserialization with options
       var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
       var entity = System.Text.Json.JsonSerializer.Deserialize<MyEntity>(jsonString, options);
       
       // Continue with normal logic
   }
   ```

2. **Check ModelState Errors**: If `!ModelState.IsValid`, return `BadRequest(ModelState)` to see validation errors.

3. **Debug Model Class**: Review the model class property types and annotations:
   - Are there required fields that might be missing?
   - Are there non-nullable value types that can't handle null or empty values?

4. **Client-Side Validation**:
   - Generate valid values for required fields
   - Remove properties that should be server-controlled (like audit fields)
   - Convert empty strings to null when appropriate

### Best Practices

1. **Server-Side Control of Audit Fields**: Never accept client values for:
   - Creation/modification timestamps
   - User IDs for audit tracking
   - Calculated fields that rely on server-side logic

2. **Client-Side JSON Structure**:
   - Only include necessary fields in the payload
   - Use proper data types (don't send empty strings where nulls are expected)
   - Generate valid GUIDs for required ID fields

3. **API Consistency**:
   - Use the same patterns across all endpoints
   - Follow OData conventions if using OData for other endpoints

### Comparing Standard OData vs. Custom Endpoints

Standard DevExtreme OData operations typically only send changed fields in updates, while custom code often includes all fields. When implementing custom endpoints, study the existing patterns used by built-in OData operations to maintain consistency.
