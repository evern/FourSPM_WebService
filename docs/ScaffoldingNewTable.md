# Scaffolding Procedure for New Database Tables

## Overview

This document outlines the step-by-step procedure for integrating a new database table into the FourSPM_WebService application. Following these steps ensures that the new table is properly configured in Entity Framework, exposed through OData, and accessible via the API.

## Prerequisites

- The database table has been created in the SQL Server database
- You have access to the FourSPM_WebService codebase
- You understand the relationships between the new table and existing tables

## Step 1: Create or Update Entity Framework (EF) Model

1. **Create the EF Model Class**
   - Create a new class in the `Data/EF/FourSPM` directory
   - Name the class to match the table name (e.g., `DOCUMENT_TYPE.cs` for the `DOCUMENT_TYPE` table)
   - Define properties that match the database columns
   - Add navigation properties for related entities

   Example:
   ```csharp
   public class DOCUMENT_TYPE
   {
       public Guid GUID { get; set; }
       public string CODE { get; set; } = string.Empty;
       public string? NAME { get; set; }
       
       // Audit fields
       public DateTime CREATED { get; set; }
       public Guid CREATEDBY { get; set; }
       public DateTime? UPDATED { get; set; }
       public Guid? UPDATEDBY { get; set; }
       public DateTime? DELETED { get; set; }
       public Guid? DELETEDBY { get; set; }
   }
   ```

2. **Update Related Entity Classes**
   - Add navigation properties to related entity classes
   - For example, add a collection property to the parent entity

3. **Update the DbContext Configuration**
   - Open `FourSPMContext.cs`
   - Add a DbSet property for the new entity
   - Configure the entity in the `OnModelCreating` method
   - Define primary key, foreign keys, and other constraints

   Example:
   ```csharp
   public virtual DbSet<DOCUMENT_TYPE> DOCUMENT_TYPEs { get; set; }
   
   // In OnModelCreating method:
   modelBuilder.Entity<DOCUMENT_TYPE>(entity =>
   {
       entity.HasKey(e => e.GUID);
       
       entity.Property(e => e.CODE)
           .IsRequired()
           .HasMaxLength(3);
           
       entity.Property(e => e.NAME)
           .HasMaxLength(500);
   });
   ```

## Step 2: Create OData Entity Model

1. **Create the OData Entity Class**
   - Create a new class in the `Data/OData/FourSPM` directory
   - Name it with the "Entity" suffix (e.g., `DocumentTypeEntity.cs`)
   - Define properties that match the EF model, using PascalCase naming (first letter capitalized)
   - Add navigation properties for related entities if needed

   Example:
   ```csharp
   public class DocumentTypeEntity
   {
       public Guid Guid { get; set; }
       public required string Code { get; set; }
       public string? Name { get; set; }
       
       // Audit fields
       public DateTime Created { get; set; }
       public Guid CreatedBy { get; set; }
       public DateTime? Updated { get; set; }
       public Guid? UpdatedBy { get; set; }
       public DateTime? Deleted { get; set; }
       public Guid? DeletedBy { get; set; }
   }
   ```

2. **Update Related OData Entity Classes**
   - Add navigation properties to related OData entity classes if needed

3. **Register the Entity in the EDM Model**
   - Open `Data/Extensions/EdmModelBuilder.cs`
   - Add the entity to the OData model

   Example:
   ```csharp
   builder.EntitySet<DocumentTypeEntity>("DocumentTypes").EntityType.HasKey(d => d.Guid);
   ```

## Step 3: Create Repository

1. **Create Repository Interface**
   - Create a new interface in the `Data/Repositories` directory
   - Name it with the "I" prefix and "Repository" suffix (e.g., `IDocumentTypeRepository.cs`)
   - Define methods for CRUD operations and any custom queries

   Example:
   ```csharp
   public interface IDocumentTypeRepository
   {
       Task<IEnumerable<DOCUMENT_TYPE>> GetAllAsync();
       Task<DOCUMENT_TYPE?> GetByIdAsync(Guid id);
       Task<DOCUMENT_TYPE> CreateAsync(DOCUMENT_TYPE documentType);
       Task<DOCUMENT_TYPE> UpdateAsync(DOCUMENT_TYPE documentType);
       Task<bool> DeleteAsync(Guid id, Guid deletedBy);
       Task<bool> ExistsAsync(Guid id);
   }
   ```

2. **Implement Repository**
   - Create a new class in the `Data/Repositories` directory
   - Name it with the "Repository" suffix (e.g., `DocumentTypeRepository.cs`)
   - Implement the repository interface
   - Use direct DbSet references rather than generic Set<T>() methods
   - Include proper error handling and entity loading

   Example:
   ```csharp
   public class DocumentTypeRepository : IDocumentTypeRepository
   {
       private readonly FourSPMContext _context;

       public DocumentTypeRepository(FourSPMContext context)
       {
           _context = context;
       }

       public async Task<IEnumerable<DOCUMENT_TYPE>> GetAllAsync()
       {
           return await _context.DOCUMENT_TYPEs
               .Where(d => d.DELETED == null)
               .OrderByDescending(d => d.CREATED)
               .ToListAsync();
       }

       public async Task<DOCUMENT_TYPE?> GetByIdAsync(Guid id)
       {
           return await _context.DOCUMENT_TYPEs
               .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);
       }

       public async Task<DOCUMENT_TYPE> CreateAsync(DOCUMENT_TYPE documentType)
       {
           documentType.CREATED = DateTime.Now;
           _context.DOCUMENT_TYPEs.Add(documentType);
           await _context.SaveChangesAsync();
           return await GetByIdAsync(documentType.GUID) ?? documentType;
       }

       public async Task<DOCUMENT_TYPE> UpdateAsync(DOCUMENT_TYPE documentType)
       {
           var existingDocumentType = await _context.DOCUMENT_TYPEs
               .FirstOrDefaultAsync(d => d.GUID == documentType.GUID && d.DELETED == null);

           if (existingDocumentType == null)
               throw new KeyNotFoundException($"Document type with ID {documentType.GUID} not found");

           existingDocumentType.CODE = documentType.CODE;
           existingDocumentType.NAME = documentType.NAME;
           existingDocumentType.UPDATED = DateTime.Now;
           existingDocumentType.UPDATEDBY = documentType.UPDATEDBY;

           await _context.SaveChangesAsync();
           return await GetByIdAsync(existingDocumentType.GUID) ?? existingDocumentType;
       }

       public async Task<bool> DeleteAsync(Guid id, Guid deletedBy)
       {
           var documentType = await _context.DOCUMENT_TYPEs
               .FirstOrDefaultAsync(d => d.GUID == id && d.DELETED == null);

           if (documentType == null)
               return false;

           documentType.DELETED = DateTime.Now;
           documentType.DELETEDBY = deletedBy;

           await _context.SaveChangesAsync();
           return true;
       }

       public async Task<bool> ExistsAsync(Guid id)
       {
           return await _context.DOCUMENT_TYPEs
               .AnyAsync(d => d.GUID == id && d.DELETED == null);
       }
   }
   ```

3. **Register the Repository**
   - Open `Extensions/ServiceExtensions.cs`
   - Add the repository to the dependency injection container

   Example:
   ```csharp
   services.AddScoped<IDocumentTypeRepository, DocumentTypeRepository>();
   ```

## Step 4: Create Controller

1. **Create Controller Class**
   - Create a new class in the `Controllers` directory
   - Name it with the "Controller" suffix (e.g., `DocumentTypesController.cs`)
   - Inherit from `FourSPMODataController`
   - Add required attributes ([Authorize], [ODataRouteComponent])
   - Implement methods for CRUD operations and OData queries

   Example:
   ```csharp
   [Authorize]
   [ODataRouteComponent("odata/v1")]
   public class DocumentTypesController : FourSPMODataController
   {
       private readonly IDocumentTypeRepository _repository;
       private readonly ApplicationUser _applicationUser;
       private readonly ILogger<DocumentTypesController> _logger;

       public DocumentTypesController(
           IDocumentTypeRepository repository,
           ApplicationUser applicationUser,
           ILogger<DocumentTypesController> logger)
       {
           _repository = repository;
           _applicationUser = applicationUser;
           _logger = logger;
       }

       [EnableQuery]
       public async Task<IActionResult> Get()
       {
           var documentTypes = await _repository.GetAllAsync();
           var entities = documentTypes.Select(d => MapToEntity(d));
           return Ok(entities);
       }

       [EnableQuery]
       public async Task<IActionResult> Get([FromRoute] Guid key)
       {
           var documentType = await _repository.GetByIdAsync(key);
           if (documentType == null)
               return NotFound();

           return Ok(MapToEntity(documentType));
       }

       // Implement other CRUD methods...

       private static DocumentTypeEntity MapToEntity(DOCUMENT_TYPE documentType)
       {
           return new DocumentTypeEntity
           {
               Guid = documentType.GUID,
               Code = documentType.CODE,
               Name = documentType.NAME,
               Created = documentType.CREATED,
               CreatedBy = documentType.CREATEDBY,
               Updated = documentType.UPDATED,
               UpdatedBy = documentType.UPDATEDBY,
               Deleted = documentType.DELETED,
               DeletedBy = documentType.DELETEDBY
           };
       }
   }
   ```

> **Important Note**: The DocumentTypes implementation should be considered the standard pattern to follow. Unlike some older implementations, it follows best practices such as:
> - Using explicit DbSet properties rather than generic Set<T>() methods
> - Proper exception handling and validation
> - Clean separation of concerns
> - Consistent naming and patterns
> - Repository methods return fully-loaded entities after modifications

## Step 5: Testing

1. **Test API Endpoints**
   - Use tools like Postman or Swagger to test the API endpoints
   - Test CRUD operations (Create, Read, Update, Delete)
   - Verify that OData query parameters work as expected

2. **Add Unit Tests**
   - Create unit tests for the repository and controller classes
   - Test edge cases and error handling

## Common Issues and Solutions

### Entity Not Registered in OData Model

**Problem**: OData endpoints return 404 Not Found

**Solution**: Ensure the entity is registered in the EDM model builder

### Repository Not Registered in DI Container

**Problem**: Controller throws an error about unresolved dependencies

**Solution**: Register the repository in the ServiceExtensions.cs file

### Navigation Properties Not Loading

**Problem**: Related entities are not included in the response

**Solution**: Use `Include()` in the repository methods to explicitly load related entities

### Incorrect JSON Property Names

**Problem**: JSON property names don't match the expected format

**Solution**: Check property naming in the OData entity class and ensure they follow PascalCase naming
