# MSAL Single Sign-On Authentication Migration Product Requirements Document

## 1. Introduction

### 1.1 Purpose of this document

This Product Requirements Document (PRD) outlines the comprehensive requirements for migrating the FourSPM application's current authentication system to Microsoft Authentication Library (MSAL) with Single Sign-On (SSO) capabilities. It details the implementation approach for both backend and frontend components, ensuring a seamless transition while maintaining application functionality.

### 1.2 Project scope

The project encompasses the integration of Azure Active Directory authentication, implementation of role-based authorization, and creation of interfaces for role and permission management. The migration will replace the existing token-based authentication while enhancing security and providing more granular access control through role-based permissions.

## 2. Product overview

### 2.1 Current system

The FourSPM application currently utilizes a custom token-based authentication system without SSO capabilities. User permissions are not managed through explicit roles, and there's no granular permission system. Authentication is handled through direct token validation without leveraging enterprise identity providers.

### 2.2 Proposed solution

The proposed solution will implement Microsoft Authentication Library (MSAL) to enable Single Sign-On capabilities through Azure Active Directory. The system will introduce role-based permission management with a user interface for administrative control. This migration will enhance security, simplify user experience through SSO, and provide more precise access control through granular permissions.

## 3. Goals and objectives

### 3.1 Primary goals

- Implement Azure AD authentication using MSAL for both frontend and backend
- Create a role-based permission system with database storage
- Develop administrative interfaces for role and permission management
- Ensure secure endpoint access based on user roles and permissions
- Maintain compatibility with existing application functionality

### 3.2 Success metrics

- Successful authentication through Azure AD
- Proper role assignment and permission validation
- Secure access control for protected endpoints
- Functioning role and permission management interfaces
- Minimal disruption to existing user experience

## 4. Target audience

### 4.1 End users

- Application users requiring seamless authentication experience
- Users with varying levels of access requirements based on their organizational roles

### 4.2 Administrators

- System administrators responsible for managing user access rights
- Role managers who define and assign appropriate permissions

### 4.3 Developers

- Backend developers implementing the MSAL authentication and authorization
- Frontend developers integrating MSAL components and building management interfaces

## 5. Features and requirements

### 5.1 Azure AD integration

- Connect to Azure AD tenant with ID: 3c7fa9e9-64e7-443c-905a-d9134ca00da9
- Use Application (client) ID: c67bf91d-8b6a-494a-8b99-c7a4592e08c1
- Implement OAuth scopes:
  - api://c67bf91d-8b6a-494a-8b99-c7a4592e08c1/Application.Admin
  - api://c67bf91d-8b6a-494a-8b99-c7a4592e08c1/Application.User
- Support environment-specific configuration for development and production

### 5.2 Database schema

- Utilize existing ROLE and ROLE_PERMISSION tables
- ROLE table defines application roles with system role capability
- ROLE_PERMISSION table maps permissions to roles
- Implement Entity Framework models for both tables

### 5.3 Role management

- Create a role management interface similar to the variations component
- Allow creation, editing, and deletion of roles
- Implement navigation to permission management for each role
- Support system roles that automatically receive all permissions

### 5.4 Permission management

- Generate static permissions based on application page structure
- Implement UI for assigning permissions to roles
- Automatically grant view permission when edit permission is assigned
- Group permissions by category for intuitive management

### 5.5 Authorization system

- Implement permission-based authorization using role claims
- Create a temporary development flag for system roles during initial development
- Support case-insensitive mapping between Azure AD claims and database roles
- Ensure home page and authentication endpoints remain accessible without authentication

## 6. User stories and acceptance criteria

### 6.1 Authentication

#### ST-101: User login via SSO
**As a** user,  
**I want to** log in using my organization's Azure AD credentials,  
**So that** I can access the application without managing separate credentials.

**Acceptance criteria:**
- User can authenticate through Azure AD
- User is properly redirected to the application after successful authentication
- Login state persists across page reloads
- User token information is securely stored
- Authentication errors are clearly communicated

#### ST-102: User logout
**As a** user,  
**I want to** log out of the application,  
**So that** my session is terminated and my credentials are no longer active.

**Acceptance criteria:**
- User can log out via logout button
- Session is properly terminated on the backend
- User is redirected to login page after logout
- Token is removed from storage

#### ST-103: Silent authentication
**As a** returning user,  
**I want to** be automatically authenticated if I have an active session,  
**So that** I don't need to log in repeatedly.

**Acceptance criteria:**
- User with valid token is automatically logged in
- Token refresh occurs transparently when needed
- No login screen is shown for authenticated users

### 6.2 Role management

#### ST-201: View roles
**As an** administrator,  
**I want to** view all available roles in the system,  
**So that** I can manage access control effectively.

**Acceptance criteria:**
- Roles are displayed in a grid view
- Grid shows role name, display name, description, and system role status
- Permissions button is available for each role

#### ST-202: Create role
**As an** administrator,  
**I want to** create new roles,  
**So that** I can define custom access levels for different user groups.

**Acceptance criteria:**
- Form is provided for entering role details
- Required fields are validated
- Role is persisted to database when saved
- New role appears in the roles grid
- System role flag can be set during creation

#### ST-203: Edit role
**As an** administrator,  
**I want to** edit existing roles,  
**So that** I can update access levels as requirements change.

**Acceptance criteria:**
- Form is pre-populated with existing role details
- Role name cannot be changed after creation
- Changes are persisted to database when saved
- Updated role information appears in the roles grid

#### ST-204: Delete role
**As an** administrator,  
**I want to** delete unnecessary roles,  
**So that** I can maintain a clean and relevant role structure.

**Acceptance criteria:**
- Role can be deleted via grid action
- Confirmation is requested before deletion
- Role and associated permissions are removed from database
- Deleted role no longer appears in the roles grid

### 6.3 Permission management

#### ST-301: View permissions for a role
**As an** administrator,  
**I want to** view all permissions assigned to a specific role,  
**So that** I can understand its current access rights.

**Acceptance criteria:**
- Navigating from roles grid shows permission management screen
- All available permissions are displayed with current selection status
- Permissions are grouped by category
- Role name is displayed in the header

#### ST-302: Modify permissions for a role
**As an** administrator,  
**I want to** modify the permissions assigned to a role,  
**So that** I can control what actions users with that role can perform.

**Acceptance criteria:**
- Permissions can be toggled via checkboxes
- View permission is automatically granted when edit permission is selected
- Changes are persisted when save button is clicked
- Success/error notifications are displayed
- Permission changes take effect immediately

#### ST-303: System role behavior
**As an** administrator,  
**I want** system roles to automatically have all permissions,  
**So that** I can create administrative roles with full access.

**Acceptance criteria:**
- Roles with system role flag receive all permissions automatically
- System roles bypass permission checks in authorization handler
- Changes to available permissions are automatically granted to system roles

### 6.4 Database modeling

#### ST-401: Role entity model
**As a** developer,  
**I want** proper Entity Framework models for the ROLE table,  
**So that** I can efficiently interact with role data.

**Acceptance criteria:**
- ROLE entity class created with all required properties
- Navigation properties to ROLE_PERMISSION implemented
- Entity correctly configured in DbContext
- Entity properly maps to database table

#### ST-402: Role permission entity model
**As a** developer,  
**I want** proper Entity Framework models for the ROLE_PERMISSION table,  
**So that** I can efficiently interact with permission data.

**Acceptance criteria:**
- ROLE_PERMISSION entity class created with all required properties
- Navigation property to ROLE implemented
- Entity correctly configured in DbContext
- Entity properly maps to database table

## 7. Technical requirements / stack

### 7.1 Backend components

- ASP.NET Core for backend API
- Entity Framework Core for database access
- Microsoft Identity Web for MSAL integration
- JWT Bearer authentication for token validation
- Custom authorization handlers for permission validation
- C# for implementation language

### 7.2 Frontend components

- React for frontend framework
- TypeScript for type safety
- @azure/msal-browser and @azure/msal-react for MSAL integration
- React Router for route protection
- DevExtreme components for UI elements
- React Context API for state management

### 7.3 Database

- SQL Server database
- ROLE and ROLE_PERMISSION tables
- Entity Framework Core migrations for schema updates

### 7.4 Authentication provider

- Azure Active Directory
- OAuth 2.0 / OpenID Connect
- MSAL for token acquisition and management

### 7.5 Environment configuration

- Separate configuration for development and production
- Environment variables for sensitive configuration
- Configuration for Azure AD parameters in appsettings.json

## 8. Design and user interface

### 8.1 Role management UI

- Grid-based interface similar to variations component
- Columns for role name, display name, description, and system role flag
- Button in each row for navigating to permission management
- Add/edit/delete functionality through grid operations
- Toast notifications for success/error feedback

### 8.2 Permission management UI

- Form-based interface for managing permissions
- Permissions grouped by category (e.g., Projects, Deliverables, etc.)
- Checkbox for each permission
- Save button for persisting changes
- Header displaying the role being managed
- Toast notifications for success/error feedback

### 8.3 Login experience

- Microsoft-branded login button
- Redirect to Microsoft login page
- Automatic redirect back to application after authentication
- Error handling for authentication failures
- Loading indicators during authentication process

### 8.4 Authorization flow

- Token acquisition through MSAL
- Token inclusion in API requests
- Permission validation on server side
- Appropriate error responses for unauthorized access
- Redirect to unauthorized page for frontend access attempts
