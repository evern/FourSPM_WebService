using FourSPM_WebService.Data.EF.FourSPM;
using FourSPM_WebService.Data.OData.FourSPM;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace FourSPM_WebService.Helpers
{
    /// <summary>
    /// Helper class for mapping deliverable domain entities to DTOs
    /// </summary>
    public static class DeliverableMapperHelper
    {
        /// <summary>
        /// Creates an expression that maps DELIVERABLE entities to DeliverableEntity DTOs for use in IQueryable projections
        /// </summary>
        /// <param name="currentVariationGuid">The current variation GUID being worked on (optional)</param>
        /// <returns>An expression that can be used in Select() with IQueryable</returns>
        public static Expression<Func<DELIVERABLE, DeliverableEntity>> GetEntityMappingExpression(Guid? currentVariationGuid = null)
        {
            return d => new DeliverableEntity
            {
                Guid = d.GUID,
                ProjectGuid = d.GUID_PROJECT,
                ClientNumber = d.Project != null && d.Project.Client != null ? d.Project.Client.NUMBER : string.Empty,
                ProjectNumber = d.Project != null ? d.Project.PROJECT_NUMBER : string.Empty,
                AreaNumber = d.AREA_NUMBER,
                Discipline = d.DISCIPLINE,
                DocumentType = d.DOCUMENT_TYPE,
                DepartmentId = d.DEPARTMENT_ID,
                DeliverableTypeId = d.DELIVERABLE_TYPE_ID,
                DeliverableGateGuid = d.GUID_DELIVERABLE_GATE,
                InternalDocumentNumber = d.INTERNAL_DOCUMENT_NUMBER,
                ClientDocumentNumber = d.CLIENT_DOCUMENT_NUMBER,
                DocumentTitle = d.DOCUMENT_TITLE,
                BudgetHours = d.BUDGET_HOURS,
                VariationHours = d.VARIATION_HOURS,
                TotalHours = d.BUDGET_HOURS + d.APPROVED_VARIATION_HOURS,
                TotalCost = d.TOTAL_COST,
                BookingCode = d.BOOKING_CODE,
                Created = d.CREATED,
                CreatedBy = d.CREATEDBY,
                Updated = d.UPDATED,
                UpdatedBy = d.UPDATEDBY,
                Deleted = d.DELETED,
                DeletedBy = d.DELETEDBY,
                
                // Map the variation fields
                VariationStatus = (VariationStatus)d.VARIATION_STATUS,
                VariationGuid = d.GUID_VARIATION,
                OriginalDeliverableGuid = d.GUID_ORIGINAL_DELIVERABLE,
                ApprovedVariationHours = d.APPROVED_VARIATION_HOURS,
                VariationName = d.Variation != null ? d.Variation.NAME : null,
                
                // Set the UI status based on variation properties
                UIStatus = d.VARIATION_STATUS == (int)VariationStatus.UnapprovedCancellation || 
                          d.VARIATION_STATUS == (int)VariationStatus.ApprovedCancellation
                    ? "Cancel"
                    : (d.GUID_VARIATION.HasValue && 
                       // Check if it belongs to a different variation than the current one
                       (currentVariationGuid.HasValue && d.GUID_VARIATION.Value != currentVariationGuid.Value))
                        ? "Original" // Treat deliverables from other variations as Original
                    : (d.GUID_VARIATION.HasValue && d.GUID_ORIGINAL_DELIVERABLE.HasValue && 
                       d.GUID_ORIGINAL_DELIVERABLE.Value != d.GUID)
                        ? "Edit"
                        : (d.GUID_VARIATION.HasValue && (!d.GUID_ORIGINAL_DELIVERABLE.HasValue || 
                           d.GUID_ORIGINAL_DELIVERABLE.Value == d.GUID))
                            ? "Add"
                            : "Original"
                
                // Note: Complex navigation properties like Project, ProgressItems, and DeliverableGate
                // cannot be included in the projection for OData filtering
                // They will be populated after materialization when needed
            };
        }
        
        /// <summary>
        /// Maps a DELIVERABLE domain entity to a DeliverableEntity DTO with optional period for progress calculation
        /// </summary>
        /// <param name="deliverable">The domain entity to map</param>
        /// <param name="period">Optional period for progress calculation</param>
        /// <param name="currentVariationGuid">The current variation GUID being worked on (optional)</param>
        /// <returns>The mapped DTO or null if the input is null</returns>
        public static DeliverableEntity? MapToEntity(DELIVERABLE? deliverable, int? period = null, Guid? currentVariationGuid = null)
        {
            if (deliverable == null) return null;
            
            // Extract client number and project number from the Project entity if available
            string clientNumber = deliverable.Project?.Client?.NUMBER ?? string.Empty;
            string projectNumber = deliverable.Project?.PROJECT_NUMBER ?? string.Empty;
            
            // Always use the database-stored value for internal document number
            string internalDocumentNumber = deliverable.INTERNAL_DOCUMENT_NUMBER;
            
            decimal totalHours = deliverable.BUDGET_HOURS + deliverable.APPROVED_VARIATION_HOURS;
            
            var validProgressItems = deliverable.ProgressItems
                .Where(p => p.DELETED == null)
                .ToList();
            
            var entity = new DeliverableEntity
            {
                Guid = deliverable.GUID,
                ProjectGuid = deliverable.GUID_PROJECT,
                ClientNumber = clientNumber,
                ProjectNumber = projectNumber,
                AreaNumber = deliverable.AREA_NUMBER,
                Discipline = deliverable.DISCIPLINE,
                DocumentType = deliverable.DOCUMENT_TYPE,
                DepartmentId = deliverable.DEPARTMENT_ID,
                DeliverableTypeId = deliverable.DELIVERABLE_TYPE_ID,
                DeliverableGateGuid = deliverable.GUID_DELIVERABLE_GATE,
                InternalDocumentNumber = internalDocumentNumber,
                ClientDocumentNumber = deliverable.CLIENT_DOCUMENT_NUMBER,
                DocumentTitle = deliverable.DOCUMENT_TITLE,
                BudgetHours = deliverable.BUDGET_HOURS,
                VariationHours = deliverable.VARIATION_HOURS,
                TotalHours = totalHours,
                TotalCost = deliverable.TOTAL_COST,
                BookingCode = deliverable.BOOKING_CODE,
                Created = deliverable.CREATED,
                CreatedBy = deliverable.CREATEDBY,
                Updated = deliverable.UPDATED,
                UpdatedBy = deliverable.UPDATEDBY,
                Deleted = deliverable.DELETED,
                DeletedBy = deliverable.DELETEDBY,
                
                // Map the variation fields
                VariationStatus = (VariationStatus)deliverable.VARIATION_STATUS, // Cast int to enum
                VariationGuid = deliverable.GUID_VARIATION,
                OriginalDeliverableGuid = deliverable.GUID_ORIGINAL_DELIVERABLE,
                ApprovedVariationHours = deliverable.APPROVED_VARIATION_HOURS,
                Project = deliverable.Project != null ? new ProjectEntity
                {
                    Guid = deliverable.Project.GUID,
                    ClientGuid = deliverable.Project.GUID_CLIENT,
                    ProjectNumber = deliverable.Project.PROJECT_NUMBER,
                    Name = deliverable.Project.NAME,
                    Client = deliverable.Project.Client != null ? new ClientEntity {
                        Guid = deliverable.Project.Client.GUID,
                        Number = deliverable.Project.Client.NUMBER,
                        Description = deliverable.Project.Client.DESCRIPTION
                    } : null
                } : null,
                ProgressItems = validProgressItems.Select(p => new ProgressEntity
                {
                    Guid = p.GUID,
                    DeliverableGuid = p.GUID_DELIVERABLE,
                    Period = p.PERIOD,
                    Units = p.UNITS,
                    Created = p.CREATED,
                    CreatedBy = p.CREATEDBY,
                    Updated = p.UPDATED,
                    UpdatedBy = p.UPDATEDBY,
                    Deleted = p.DELETED,
                    DeletedBy = p.DELETEDBY
                }).ToList(),
                DeliverableGate = deliverable.DeliverableGate != null ? new DeliverableGateEntity
                {
                    Guid = deliverable.DeliverableGate.GUID,
                    Name = deliverable.DeliverableGate.NAME,
                    MaxPercentage = deliverable.DeliverableGate.MAX_PERCENTAGE,
                    AutoPercentage = deliverable.DeliverableGate.AUTO_PERCENTAGE
                } : null,
                Variation = deliverable.Variation != null ? new VariationEntity
                {
                    Guid = deliverable.Variation.GUID,
                    Name = deliverable.Variation.NAME,
                    ProjectGuid = deliverable.Variation.GUID_PROJECT,
                    Comments = deliverable.Variation.COMMENTS,
                    Submitted = deliverable.Variation.SUBMITTED,
                    ClientApproved = deliverable.Variation.CLIENT_APPROVED
                } : null
            };
            
            // Apply UI status based on the current variation
            SetUIStatus(entity, currentVariationGuid);
            
            // Calculate progress percentages if period is provided
            if (period.HasValue)
            {
                CalculateProgressPercentages(entity, period.Value);
            }
            
            return entity;
        }
        
        /// <summary>
        /// Sets the UIStatus property of a deliverable entity based on its variation properties
        /// </summary>
        /// <param name="entity">The deliverable entity to update</param>
        /// <param name="currentVariationGuid">The current variation GUID being worked on (optional)</param>
        private static void SetUIStatus(DeliverableEntity entity, Guid? currentVariationGuid = null)
        {
            if (entity == null) return;
            
            // Set UI status based on variation status and original deliverable relationship
            if (entity.VariationStatus == VariationStatus.UnapprovedCancellation || 
                entity.VariationStatus == VariationStatus.ApprovedCancellation)
            {
                entity.UIStatus = "Cancel";
            }
            // Check if it belongs to a different variation than the current one
            else if (entity.VariationGuid.HasValue && currentVariationGuid.HasValue && 
                     entity.VariationGuid.Value != currentVariationGuid.Value)
            {
                // Deliverable from another variation - treat as Original
                entity.UIStatus = "Original";
            } 
            else if (entity.VariationGuid.HasValue && entity.OriginalDeliverableGuid.HasValue && 
                    entity.OriginalDeliverableGuid.Value != entity.Guid) 
            {
                // Modified deliverable (has both variationGuid and originalDeliverableGuid pointing to different records)
                entity.UIStatus = "Edit";
            }
            else if (entity.VariationGuid.HasValue && (!entity.OriginalDeliverableGuid.HasValue || 
                    entity.OriginalDeliverableGuid.Value == entity.Guid))
            {
                // New deliverable added to variation (has variationGuid but no originalDeliverableGuid or it points to itself)
                entity.UIStatus = "Add";
            } 
            else 
            {
                entity.UIStatus = "Original";
            }
        }
        
        /// <summary>
        /// Calculates progress percentages for a deliverable entity
        /// </summary>
        private static void CalculateProgressPercentages(DeliverableEntity entity, int period)
        {
            if (entity == null || entity.ProgressItems == null) return;
            
            // Default values for all percentages and hours
            entity.PreviousPeriodEarntPercentage = 0;
            entity.CurrentPeriodEarntPercentage = 0;
            entity.FuturePeriodEarntPercentage = 0;
            entity.CumulativeEarntPercentage = 0;
            entity.TotalPercentageEarnt = 0;
            entity.CurrentPeriodEarntHours = 0;
            entity.TotalEarntHours = 0;
            
            // If no progress items or no hours, we can't calculate percentages
            if (!entity.ProgressItems.Any() || entity.TotalHours <= 0)
            {
                return;
            }

            var validProgressItems = entity.ProgressItems.Where(item => item.Deleted == null).ToList();
            
            // Calculate cumulative percentage earned up to the current period
            var currentPeriodItems = validProgressItems
                .Where(item => item.Period <= period)
                .ToList();
                
            if (currentPeriodItems.Any())
            {
                decimal currentPeriodUnits = currentPeriodItems.Sum(item => item.Units);
                entity.CumulativeEarntPercentage = currentPeriodUnits / entity.TotalHours;
            }
            
            // Calculate previous period earned percentage
            var previousPeriodItems = validProgressItems
                .Where(item => item.Period < period)
                .ToList();
            
            if (previousPeriodItems.Any())
            {
                decimal previousPeriodUnits = previousPeriodItems.Sum(item => item.Units);
                entity.PreviousPeriodEarntPercentage = previousPeriodUnits / entity.TotalHours;
            }
            
            // Calculate current period percentage (the difference)
            decimal currentPeriodPercentage = Math.Max(0, entity.CumulativeEarntPercentage - entity.PreviousPeriodEarntPercentage);
            entity.CurrentPeriodEarntPercentage = currentPeriodPercentage;
            
            // Calculate earned hours for the current period only
            entity.CurrentPeriodEarntHours = entity.TotalHours * currentPeriodPercentage;
            
            // Calculate total percentage earned (across all periods)
            decimal totalUnits = validProgressItems.Sum(item => item.Units);
            entity.TotalPercentageEarnt = totalUnits / entity.TotalHours;
            entity.TotalEarntHours = entity.TotalHours * entity.TotalPercentageEarnt;
            
            // Calculate future period earned percentage
            var futurePeriodItems = validProgressItems
                .Where(item => item.Period > period)
                .ToList();
            
            if (futurePeriodItems.Any())
            {
                // Get the earliest future period
                var minFuturePeriod = futurePeriodItems.Min(item => item.Period);
                var futurePeriodItem = futurePeriodItems.FirstOrDefault(item => item.Period == minFuturePeriod);
                
                if (futurePeriodItem != null)
                {
                    entity.FuturePeriodEarntPercentage = futurePeriodItem.Units / entity.TotalHours;
                }
            }
        }
    }
}
