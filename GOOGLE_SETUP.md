# Google Business Profile API Setup Guide

This guide explains what information you need from your Google account to set up the Google Connector Service.

## Required Information from Google Account

### 1. Google Cloud Project
- **Project Name**: A name for your Google Cloud project
- **Project ID**: Automatically generated or custom project ID

### 2. Service Account Credentials (JSON)
You need to create a **Service Account** and download its credentials JSON file. This contains:

- **type**: "service_account"
- **project_id**: Your Google Cloud project ID
- **private_key_id**: Unique identifier for the private key
- **private_key**: RSA private key (keep this secure!)
- **client_email**: Service account email (format: `{service-account-name}@{project-id}.iam.gserviceaccount.com`)
- **client_id**: Numeric client ID
- **auth_uri**: OAuth2 authorization endpoint
- **token_uri**: OAuth2 token endpoint
- **auth_provider_x509_cert_url**: Provider certificate URL
- **client_x509_cert_url**: Client certificate URL

### 3. Google Business Profile Location Information
For each business location you want to connect, you need:

- **Account ID**: The Google Business Profile account ID (format: `accounts/{accountId}`)
- **Location ID**: The specific location ID (format: `locations/{locationId}`)
- **Full Path**: `accounts/{accountId}/locations/{locationId}`

## Step-by-Step Setup Instructions

### Step 1: Create Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click on the project dropdown and select "New Project"
3. Enter a project name and click "Create"
4. Note your **Project ID**

### Step 2: Enable Google Business Profile API

1. In your Google Cloud project, go to **APIs & Services** > **Library**
2. Search for "Google Business Profile API" (formerly My Business API)
3. Click on it and click **Enable**
4. Also enable "Google Business Profile Performance API" if available

### Step 3: Create Service Account

1. Go to **IAM & Admin** > **Service Accounts**
2. Click **Create Service Account**
3. Enter a name (e.g., "google-connector-service")
4. Enter a description (optional)
5. Click **Create and Continue**
6. Skip role assignment for now (or assign "Service Account User")
7. Click **Done**

### Step 4: Create and Download Service Account Key

1. Click on the service account you just created
2. Go to the **Keys** tab
3. Click **Add Key** > **Create new key**
4. Select **JSON** format
5. Click **Create** - the JSON file will download automatically
6. **IMPORTANT**: Store this file securely - it contains private keys!

### Step 5: Grant API Access to Service Account

1. In the Google Cloud Console, go to **APIs & Services** > **Credentials**
2. Find your service account
3. Click on it to view details
4. Note the **Service Account Email** (e.g., `google-connector-service@your-project-id.iam.gserviceaccount.com`)

### Step 6: Link Service Account to Google Business Profile

**Important**: Service accounts need to be granted access to specific Google Business Profile locations.

1. Go to [Google Business Profile Manager](https://business.google.com/)
2. Select the business location you want to connect
3. Go to **Settings** > **Users**
4. Click **Add users**
5. Enter the **Service Account Email** from Step 5
6. Grant appropriate permissions (at minimum: "Manager" or "Owner" role)
7. Click **Invite**

### Step 7: Get Your Business Location Information

1. In Google Business Profile Manager, select your location
2. The location ID can be found in the URL or by using the Google Business Profile API
3. You can also use the [Google Business Profile API Explorer](https://developers.google.com/my-business/content/overview) to discover your account and location IDs

**Format**: `accounts/{accountId}/locations/{locationId}`

Example:
- Account ID: `12345678901234567890`
- Location ID: `12345678901234567890`
- Full path: `accounts/12345678901234567890/locations/12345678901234567890`

## Configuration

### Local Development (local.settings.json)

```json
{
  "Values": {
    "Google:CredentialsJson": "{\"type\":\"service_account\",\"project_id\":\"your-project-id\",\"private_key_id\":\"...\",\"private_key\":\"-----BEGIN PRIVATE KEY-----\\n...\\n-----END PRIVATE KEY-----\\n\",\"client_email\":\"your-service-account@your-project-id.iam.gserviceaccount.com\",\"client_id\":\"...\",\"auth_uri\":\"https://accounts.google.com/o/oauth2/auth\",\"token_uri\":\"https://oauth2.googleapis.com/token\",\"auth_provider_x509_cert_url\":\"https://www.googleapis.com/oauth2/v1/certs\",\"client_x509_cert_url\":\"https://www.googleapis.com/robot/v1/metadata/x509/your-service-account%40your-project-id.iam.gserviceaccount.com\"}"
  }
}
```

**Security Note**: For production, store the credentials JSON in Azure Key Vault or Azure App Configuration instead of directly in configuration files.

### Azure Key Vault (Recommended for Production)

1. Create an Azure Key Vault
2. Store the credentials JSON as a secret named `Google-CredentialsJson`
3. Update your Function App configuration to reference the Key Vault

## API Scopes Required

The service account needs these OAuth scopes:
- `https://www.googleapis.com/auth/business.manage` - Manage Google Business Profile
- `https://www.googleapis.com/auth/businessprofileperformance` - Access performance data

These are automatically requested when using the service account credentials.

## Testing the Connection

1. Use the **Link Business** endpoint to connect a profile service Business to a Google Business Profile location
2. Use the **Ingest Reviews** endpoint to fetch reviews from Google
3. Check logs for any authentication or API errors

## Troubleshooting

### Common Issues

1. **"Failed to create service account credential"**
   - Ensure the JSON credentials file is valid
   - Check that it's a service account (not OAuth client credentials)

2. **"403 Forbidden" or "Permission Denied"**
   - Verify the service account email has been added to Google Business Profile users
   - Check that the service account has the correct permissions
   - Ensure the Google Business Profile API is enabled

3. **"404 Not Found" for location**
   - Verify the location ID is correct
   - Ensure you're using the full path format: `accounts/{accountId}/locations/{locationId}`
   - Check that the service account has access to that specific location

4. **"Invalid credentials"**
   - Verify the JSON credentials are correctly formatted
   - Check that the service account hasn't been deleted
   - Ensure the private key hasn't been rotated

## Additional Resources

- [Google Business Profile API Documentation](https://developers.google.com/my-business/content/overview)
- [Service Account Authentication](https://cloud.google.com/iam/docs/service-accounts)
- [Google Cloud Console](https://console.cloud.google.com/)

