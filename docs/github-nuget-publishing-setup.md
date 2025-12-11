# GitHub Actions NuGet Publishing Setup Guide

This guide provides step-by-step instructions for configuring your GitHub repository to build and publish NuGet packages to both NuGet.org and GitHub Packages.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Step 1: Create NuGet.org API Key](#step-1-create-nugetorg-api-key)
- [Step 2: Configure GitHub Secrets](#step-2-configure-github-secrets)
- [Step 3: Configure GitHub Variables](#step-3-configure-github-variables)
- [Step 4: Test the Workflow](#step-4-test-the-workflow)
- [Step 5: Publishing Workflow](#step-5-publishing-workflow)
- [Security Best Practices](#security-best-practices)
- [Troubleshooting](#troubleshooting)

---

## Overview

The GitHub Actions workflow (`.github/workflows/nuget-publish.yml`) automates:

- Building the .NET solution
- Running tests
- Creating NuGet packages for both projects
- Publishing to NuGet.org
- Publishing to GitHub Packages
- Creating GitHub releases with package attachments

The workflow triggers on:
- **Tag push**: When you push a version tag (e.g., `v1.0.0`)
- **Manual trigger**: Via GitHub Actions UI with custom version

---

## Prerequisites

1. **NuGet.org Account**: [Sign up](https://www.nuget.org/users/account/LogOn) if you don't have one
2. **GitHub Account**: With admin access to the repository
3. **Repository Access**: Admin permissions to configure secrets and variables

---

## Step 1: Create NuGet.org API Key

### 1.1 Sign in to NuGet.org

1. Go to [https://www.nuget.org](https://www.nuget.org)
2. Click **Sign in** (top-right corner)
3. Sign in with your Microsoft account

### 1.2 Generate API Key

1. Click your **username** (top-right) → **API Keys**
2. Click **+ Create** button
3. Configure the API key:
   
   **Key Name**: `GitHub-Actions-Avalonia-TestRecorder`
   
   **Owner**: Select your account or organization
   
   **Glob Pattern**: Enter the package IDs (use wildcards):
   ```
   Avalonia.TestRecorder*
   ```
   This allows publishing:
   - `Avalonia.TestRecorder`
   - `Avalonia.HeadlessTestKit`
   - Any future packages starting with `Avalonia.TestRecorder`
   
   **Expires In**: Choose appropriate duration (recommended: 365 days or 1 year)
   
   **Scopes**: Select:
   - ✅ **Push new packages and package versions**
   - ✅ **Push only new package versions** (optional)
   - ❌ **Unlist packages** (not needed)

4. Click **Create**
5. **IMPORTANT**: Copy the generated API key immediately
   - It will look like: `oy2abc...xyz` (64 characters)
   - **You cannot view it again after closing this page**
   - Store it temporarily in a secure location

### 1.3 Security Notes

- ⚠️ **Never commit the API key to your repository**
- ⚠️ **Never share the API key publicly**
- 🔒 Store it only in GitHub Secrets (next step)
- 🔄 Rotate keys periodically (every 6-12 months)
- 📝 Use descriptive names to track key usage

---

## Step 2: Configure GitHub Secrets

Secrets are encrypted environment variables used for sensitive data like API keys.

### 2.1 Navigate to Repository Secrets

1. Go to your GitHub repository
2. Click **Settings** tab (top navigation)
3. In the left sidebar, click **Secrets and variables** → **Actions**
4. You'll see two tabs: **Secrets** and **Variables**

### 2.2 Add NuGet API Key Secret

1. In the **Secrets** tab, click **New repository secret**
2. Configure the secret:

   **Name**: 
   ```
   NUGET_API_KEY
   ```
   
   **Value**: Paste the API key from Step 1.2
   
   Example value format:
   ```
   oy2abc1234567890abcdef1234567890abcdef1234567890abcdef1234
   ```

3. Click **Add secret**

### 2.3 Summary

That's it! Package metadata (Authors, Company, Description) is now configured directly in the `.csproj` files, so you don't need to set up GitHub Variables.

**To modify package metadata**, edit the following files:
- `src/Avalonia.HeadlessTestKit/Avalonia.HeadlessTestKit.csproj`
- `src/Avalonia.TestRecorder/Avalonia.TestRecorder.csproj`

Look for the `<!-- NuGet Package Metadata -->` section in each file.

---

## Step 3: Test the Workflow

Before publishing to production, test the workflow.

### 4.1 Manual Test Run

1. Go to **Actions** tab in your repository
2. Click **Build and Publish NuGet Packages** workflow
3. Click **Run workflow** button
4. Fill in the inputs:
   - **Use workflow from**: `main` (or your branch)
   - **Version number**: `1.0.0-test` (use `-test` suffix)
   - **Publish to NuGet.org**: ❌ Uncheck (for testing)
   - **Publish to GitHub Packages**: ✅ Check
5. Click **Run workflow**

### 3.2 Monitor Execution

1. Click on the running workflow
2. Watch each step execute:
   - ✅ Checkout code
   - ✅ Setup .NET
   - ✅ Restore dependencies
   - ✅ Build solution
   - ✅ Run tests
   - ✅ Pack packages
   - ✅ Publish to GitHub Packages (if enabled)

### 3.3 Verify Artifacts

1. After successful completion, check **Artifacts** section
2. Download `nuget-packages-1.0.0-test`
3. Extract and verify `.nupkg` files are present

### 3.4 Verify GitHub Packages

1. Go to repository main page
2. Look for **Packages** in the right sidebar
3. You should see both packages listed

---

## Step 4: Publishing Workflow

### 4.1 Publishing via Git Tags (Recommended)

This is the production publishing method.

**Step-by-step**:

1. **Ensure code is ready**:
   ```bash
   git status
   git pull origin main
   ```

2. **Create and push version tag**:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. **Workflow triggers automatically**:
   - Builds the solution
   - Runs all tests
   - Creates packages with version `1.0.0`
   - Publishes to both NuGet.org and GitHub Packages
   - Creates GitHub Release with packages attached

4. **Verify publication**:
   - Check [NuGet.org](https://www.nuget.org/packages?q=Avalonia.TestRecorder)
   - Check repository Packages
   - Check GitHub Releases

### 4.2 Publishing via Manual Trigger

For ad-hoc releases or testing:

1. Go to **Actions** → **Build and Publish NuGet Packages**
2. Click **Run workflow**
3. Configure:
   - **Version**: `1.0.1`
   - **Publish to NuGet.org**: ✅ Yes
   - **Publish to GitHub Packages**: ✅ Yes
4. Click **Run workflow**

### 4.3 Version Numbering

Follow [Semantic Versioning](https://semver.org/):

- **Major.Minor.Patch** (e.g., `1.0.0`)
- **Major**: Breaking changes
- **Minor**: New features (backward compatible)
- **Patch**: Bug fixes
- **Pre-release**: Use suffixes like `1.0.0-alpha`, `1.0.0-beta`, `1.0.0-rc1`

---

## Security Best Practices

### 1. Secret Management

✅ **DO**:
- Store all API keys in GitHub Secrets
- Use scoped API keys (limit to specific packages)
- Set expiration dates on API keys
- Use different keys for different projects
- Rotate keys regularly (every 6-12 months)
- Review secret access logs periodically

❌ **DON'T**:
- Commit secrets to repository
- Share secrets via email/chat
- Use keys with excessive permissions
- Use personal access tokens when API keys suffice
- Grant write access to untrusted collaborators

### 2. Repository Settings

1. **Branch Protection**:
   - Go to **Settings** → **Branches**
   - Add rule for `main` branch
   - Enable: "Require pull request reviews before merging"
   - Enable: "Require status checks to pass"

2. **Workflow Permissions**:
   - Go to **Settings** → **Actions** → **General**
   - Scroll to "Workflow permissions"
   - Select: "Read and write permissions"
   - Enable: "Allow GitHub Actions to create and approve pull requests"

3. **Secret Access**:
   - Only repository admins can add/edit secrets
   - Secrets are not exposed in workflow logs
   - Secrets are masked in output

### 3. NuGet Package Security

- **Sign packages**: Consider using package signing
- **Enable 2FA**: On your NuGet.org account
- **Reserve package IDs**: Claim your package prefix
- **Monitor downloads**: Watch for suspicious activity

### 4. Audit Trail

- Review **Actions** tab regularly for unexpected runs
- Check **Settings** → **Audit log** for configuration changes
- Monitor NuGet.org download statistics

---

## Troubleshooting

### Issue 1: "401 Unauthorized" when publishing to NuGet.org

**Cause**: Invalid or expired API key

**Solution**:
1. Generate new API key on NuGet.org
2. Update `NUGET_API_KEY` secret in GitHub
3. Ensure glob pattern matches your package IDs
4. Verify "Push" scope is enabled

### Issue 2: "403 Forbidden" when publishing to GitHub Packages

**Cause**: Insufficient permissions

**Solution**:
1. Check workflow has `packages: write` permission (line 35 in workflow)
2. Verify repository **Settings** → **Actions** → **General** → "Workflow permissions" is set to "Read and write"
3. Ensure you're using `GITHUB_TOKEN` (not a personal access token)

### Issue 3: Package version already exists

**Cause**: Attempting to republish same version

**Solution**:
1. Increment version number
2. Delete tag and re-push with new version:
   ```bash
   git tag -d v1.0.0
   git push origin :refs/tags/v1.0.0
   git tag v1.0.1
   git push origin v1.0.1
   ```
3. Note: NuGet.org doesn't allow deleting/replacing versions

### Issue 4: Build or test failures

**Cause**: Code issues or dependency problems

**Solution**:
1. Test locally first:
   ```bash
   dotnet restore
   dotnet build --configuration Release
   dotnet test --configuration Release
   ```
2. Check workflow logs for specific error messages
3. Ensure all dependencies are restored

### Issue 5: Variables not substituted in package metadata

**Cause**: Variables not configured in GitHub

**Solution**:
1. Verify all required variables exist in **Settings** → **Secrets and variables** → **Actions** → **Variables**
2. Check variable names match exactly (case-sensitive)
3. Re-run workflow after adding missing variables

### Issue 6: Workflow doesn't trigger on tag push

**Cause**: Tag pattern doesn't match

**Solution**:
1. Ensure tag follows pattern `v*.*.*` (e.g., `v1.0.0`)
2. Check **.github/workflows/nuget-publish.yml** exists
3. Verify workflow syntax is valid
4. Check **Actions** tab for any disabled workflows

---

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [NuGet Package Publishing Guide](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [Semantic Versioning](https://semver.org/)
- [.NET Pack Command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-pack)
- [GitHub Packages for NuGet](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry)

---

## Quick Reference: Commands

### Create and push a version tag:
```bash
git tag v1.0.0
git push origin v1.0.0
```

### Delete a tag (local and remote):
```bash
git tag -d v1.0.0
git push origin :refs/tags/v1.0.0
```

### List all tags:
```bash
git tag -l
```

### Test package locally:
```bash
dotnet pack --configuration Release --output ./packages
```

### Install package from GitHub Packages:
```bash
dotnet nuget add source "https://nuget.pkg.github.com/YOUR_USERNAME/index.json" --name github
dotnet add package Avalonia.TestRecorder --version 1.0.0
```

---

## Support

If you encounter issues not covered in this guide:

1. Check the **Actions** tab logs for detailed error messages
2. Review [GitHub Actions documentation](https://docs.github.com/en/actions)
3. Check [NuGet.org status page](https://status.nuget.org/)
4. Open an issue in the repository with:
   - Workflow run URL
   - Error messages
   - Steps to reproduce

---

**Last Updated**: December 2025
