# CheckinBlaze - Employee Emergency Check-in Application

## Project Overview
CheckinBlaze is a secure, authenticated web application that enables employees to quickly report their safety status ('check-in') during emergencies such as earthquakes. The application captures critical information including timestamps, optional geolocation data, optional notes, and employee safety status (OK or needs assistance). It integrates with Microsoft Graph API for advanced user insights and targeted notifications, with data stored securely in Azure Table Storage and authentication leveraging Azure Active Directory.

## Technology Stack
- **Frontend:** Blazor WebAssembly (WASM)
- **Backend:** Azure Functions (Serverless, C#)
- **Authentication:** Azure AD via Microsoft Graph API
- **Data Storage:** Azure Table Storage
- **Communication:** Microsoft Teams integration
- **Deployment/Dev Tools:** Visual Studio Code with Azure Extensions

## Authentication and Tenant Details
- **Tenant:** r6jd.onmicrosoft.com
- **Tenant ID:** e8ce889a-ba5e-4fbc-ae63-d095a9b60d95
- **App Registration ID:** 4b781c1c-0b04-4c3b-ad66-427458e9f98d

## Project Structure
```
checkinBlaze/
│
├── src/
│   ├── CheckinBlaze.Client/            # Blazor WebAssembly Frontend
│   │   ├── wwwroot/                    # Static web assets
│   │   ├── Pages/                      # Razor Pages
│   │   ├── Shared/                     # Shared Components
│   │   ├── Models/                     # Client-side models
│   │   ├── Services/                   # Client-side services
│   │   └── Program.cs                  # Application entry point
│   │
│   ├── CheckinBlaze.Functions/         # Azure Functions Backend
│   │   ├── Functions/                  # API Functions
│   │   ├── Models/                     # Server-side models
│   │   ├── Services/                   # Server-side services
│   │   └── local.settings.json         # Local settings (gitignored)
│   │
│   └── CheckinBlaze.Shared/            # Shared code between Client and Functions
│       ├── Models/                     # Shared model classes
│       ├── DTOs/                       # Data Transfer Objects
│       └── Constants/                  # Shared constants and enums
│
├── tests/                              # Test projects
│   ├── CheckinBlaze.Client.Tests/      # Frontend tests
│   ├── CheckinBlaze.Functions.Tests/   # Backend tests
│   └── CheckinBlaze.Shared.Tests/      # Shared code tests
│
├── docs/                               # Documentation
│   ├── architecture/                   # Architectural diagrams
│   └── api/                            # API documentation
│
├── .gitignore                          # Git ignore file
├── azure-pipelines.yml                 # CI/CD pipeline configuration
├── global.json                         # .NET SDK version
└── README.md                           # Project readme
```

## Core Features
1. **User Authentication**
   - Azure AD authentication with MFA
   - Microsoft Graph API integration

2. **Check-in Functionality**
   - Instant check-in with geolocation capture
   - Optional notes and safety status (OK or needs assistance)
   - Adjustable location precision (city-wide or precise)

3. **Microsoft Graph API Integration**
   - User search and retrieval
   - Manager/direct reports hierarchy
   - Headcount campaign initiation

4. **Teams Integration**
   - Notifications for headcount campaigns
   - Links to check-in directly from Teams

5. **Data Visualization**
   - Map display of employee check-ins
   - Breadcrumb trail of historical check-ins
   - Dashboard for team response rates

6. **Check-in Resolution Workflow**
   - Status tracking (submitted, acknowledged, resolved)
   - Clear visualization of check-in status

## Setup Instructions

### Prerequisites
1. .NET 9.0 SDK or later
2. Azure subscription
3. Azure CLI
4. Node.js and npm (for frontend tooling)
5. Visual Studio Code with recommended extensions:
   - Azure Functions
   - Azure Storage
   - C#
   - Blazor WASM

### Local Development Setup
1. Clone the repository
   ```
   git clone https://github.com/yourusername/checkinBlaze.git
   cd checkinBlaze
   ```

2. Azure AD App Registration setup
   - Register the application in your Azure AD tenant
   - Configure the redirect URIs for local development
   - Create client secrets and note them for later use

3. Configure local environment
   - Copy `src/CheckinBlaze.Functions/local.settings.json.example` to `local.settings.json`
   - Update with your Azure AD and Storage account details
   - Configure your client application settings

4. Run the solution
   - Start the Azure Functions project
   - Start the Blazor WASM client project

### Deployment
1. Deploy Azure Resources
   - Azure Functions App
   - Azure Storage Account
   - Azure AD App Registration (if not already done)

2. Configure CI/CD Pipeline
   - Use provided `azure-pipelines.yml` or GitHub Actions workflows

## Data Storage Schema
- **CheckInRecords**: User ID, timestamp, GPS coordinates, notes, status, precision level
- **UserPreferences**: Stores individual preferences
- **AuditLogs**: Records preference changes
- **HeadcountCampaigns**: Tracks headcount requests, response rates, and status indicators

## Security Considerations
- All data encrypted at rest and in transit
- Azure AD authentication with MFA (IdP controlled)
- Role-based access control
- Secure storage of sensitive information

## Contributing
Please read [CONTRIBUTING.md](./CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.