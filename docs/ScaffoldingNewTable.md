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
   - Name the class to match the table name (e.g., `PROGRESS.cs` for the `PROGRESS` table)
   - Define properties that match the database columns
   - Add navigation properties for related entities

   Example:
   ```csharp
   public class PROGRESS
   {
       public Guid GUID { get; set; }
       public Guid GUID_DELIVERABLE { get; set; }
       public DateTime? PERIOD { get; set; }
       public decimal? UNITS { get; set; }
       
       // Audit fields
       public DateTime? CREATED { get; set; }
       public Guid? CREATEDBY { get; set; }
       public DateTime? UPDATED { get; set; }
       public Guid? UPDATEDBY { get; set; }
       public DateTime? DELETED { get; set; }
       public Guid? DELETEDBY { get; set; }
       
       // Navigation properties
       public virtual DELIVERABLE? Deliverable { get; set; }
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
   public virtual DbSet<PROGRESS> PROGRESSes { get; set; }
   
   // In OnModelCreating method:
   modelBuilder.Entity<PROGRESS>(entity =>
   {
       entity.HasKey(e => e.GUID);
       
       entity.HasOne(d => d.Deliverable)
           .WithMany(p => p.ProgressItems)
           .HasForeignKey(d => d.GUID_DELIVERABLE);
   });
   ```

## Step 2: Create OData Entity Model

1. **Create the OData Entity Class**
   - Create a new class in the `Data/OData/FourSPM` directory
   - Name it with the "Entity" suffix (e.g., `ProgressEntity.cs`)
   - Define properties that match the EF model, using camelCase naming
   - Add navigation properties for related entities

   Example:
   ```csharp
   public class ProgressEntity
   {
       public Guid Guid { get; set; }
       public Guid DeliverableGuid { get; set; }
       public DateTime? Period { get; set; }
       public decimal? Units { get; set; }
       
       // Audit fields
       public DateTime? Created { get; set; }
       public Guid? CreatedBy { get; set; }
       public DateTime? Updated { get; set; }
       public Guid? UpdatedBy { get; set; }
       public DateTime? Deleted { get; set; }
       public Guid? DeletedBy { get; set; }
       
       // Navigation properties
       public virtual DeliverableEntity? Deliverable { get; set; }
   }
   ```

2. **Update Related OData Entity Classes**
   - Add navigation properties to related OData entity classes

3. **Register the Entity in the EDM Model**
   - Open `Data/Extensions/EdmModelBuilder.cs`
   - Add the entity to the OData model

   Example:
   ```csharp
   builder.EntitySet<ProgressEntity>("Progress").EntityType.HasKey(p => p.Guid);
   ```

## Step 3: Create Repository

1. **Create Repository Interface**
   - Create a new interface in the `Data/Repositories` directory
   - Name it with the "I" prefix and "Repository" suffix (e.g., `IProgressRepository.cs`)
   - Define methods for CRUD operations and any custom queries

   Example:
   ```csharp
   public interface IProgressRepository
   {
       Task<IEnumerable<PROGRESS>> GetAllAsync();
       Task<IEnumerable<PROGRESS>> GetByDeliverableIdAsync(Guid deliverableId);
       Task<PROGRESS?> GetByIdAsync(Guid id);
       Task<PROGRESS> CreateAsync(PROGRESS progress);
       Task<PROGRESS> UpdateAsync(PROGRESS progress);
       Task<bool> DeleteAsync(Guid id, Guid deletedBy);
   }
   ```

2. **Implement Repository**
   - Create a new class in the `Data/Repositories` directory
   - Name it with the "Repository" suffix (e.g., `ProgressRepository.cs`)
   - Implement the repository interface
   - Include proper error handling and entity loading

   Example:
   ```csharp
   public class ProgressRepository : IProgressRepository
   {
       private readonly FourSPMContext _context;

       public ProgressRepository(FourSPMContext context)
       {
           _context = context;
       }

       public async Task<IEnumerable<PROGRESS>> GetAllAsync()
       {
           return await _context.PROGRESSes
               .Include(p => p.Deliverable)
               .Where(p => p.DELETED == null)
               .ToListAsync();
       }

       // Implement other methods...
   }
   ```

3. **Register the Repository**
   - Open `Extensions/ServiceExtensions.cs`
   - Add the repository to the dependency injection container

   Example:
   ```csharp
   services.AddScoped<IProgressRepository, ProgressRepository>();
   ```

## Step 4: Create Controller

1. **Create Controller Class**
   - Create a new class in the `Controllers` directory
   - Name it with the "Controller" suffix (e.g., `ProgressController.cs`)
   - Inherit from `FourSPMODataController`
   - Implement methods for CRUD operations and OData queries

   Example:
   ```csharp
   [Authorize]
   [ODataRouteComponent("odata/v1")]
   public class ProgressController : FourSPMODataController
   {
       private readonly IProgressRepository _repository;
       private readonly ILogger<ProgressController> _logger;

       public ProgressController(IProgressRepository repository, ILogger<ProgressController> logger)
       {
           _repository = repository;
           _logger = logger;
       }

       [EnableQuery]
       public async Task<IActionResult> Get()
       {
           var progressItems = await _repository.GetAllAsync();
           var entities = progressItems.Select(p => MapToEntity(p));
           return Ok(entities);
       }

       // Implement other methods...

       private static ProgressEntity MapToEntity(PROGRESS progress)
       {
           // Mapping logic...
       }
   }
   ```

## Step 5: Testing

1. **Build the Application**
   - Ensure the application builds without errors

2. **Test the API Endpoints**
   - Test all CRUD operations using a tool like Postman or Swagger
   - Verify that OData queries work correctly
   - Test relationships and navigation properties

3. **Verify Error Handling**
   - Test edge cases and error conditions
   - Ensure proper error responses are returned

## Common Issues and Solutions

1. **Missing Required Properties**
   - When creating OData entities with required properties, ensure all required properties are set in object initializers
   - Example error: "Required member 'Entity.Property' must be set in the object initializer"

2. **Navigation Property Configuration**
   - Ensure navigation properties are correctly configured in both the EF model and OData model
   - Check that foreign key relationships are properly defined in the DbContext

3. **OData Model Registration**
   - If the entity is not appearing in OData queries, verify it's registered in the EdmModelBuilder

4. **Repository Registration**
   - If dependency injection fails, check that the repository is registered in ServiceExtensions.cs

## Conclusion

Following this scaffolding procedure ensures consistent implementation of new database tables in the FourSPM_WebService application. This approach maintains the application's architecture and coding standards while providing a clear path for extending the data model.
