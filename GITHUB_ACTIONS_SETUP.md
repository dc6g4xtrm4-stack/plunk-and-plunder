# GitHub Actions Setup for Automated Builds

This repo is configured to automatically build Windows executables when you create a new version tag!

## One-Time Setup: Unity License

GitHub Actions needs your Unity license to build. Here's how to set it up:

### Step 1: Get Your Unity License File

#### Option A: Personal License (Free)
1. Open Unity Hub
2. Go to **Preferences → Licenses**
3. Click on your license → **Save License File**
4. Save as `Unity_v2022.x.ulf`

#### Option B: Activate via Command Line
```bash
# Request activation file
unity-editor -quit -batchmode -createManualActivationFile

# Upload the .alf file to: https://license.unity3d.com/manual
# Download the .ulf file you receive
```

### Step 2: Add Secrets to GitHub

1. Go to your repo: https://github.com/dc6g4xtrm4-stack/plunk-and-plunder
2. Click **Settings → Secrets and variables → Actions**
3. Click **"New repository secret"** and add these three secrets:

#### Secret 1: UNITY_LICENSE
- **Name:** `UNITY_LICENSE`
- **Value:** Copy the **entire contents** of your `.ulf` file
  - Open the .ulf file in Notepad
  - Select all (Ctrl+A) and copy (Ctrl+C)
  - Paste into the secret value field

#### Secret 2: UNITY_EMAIL
- **Name:** `UNITY_EMAIL`
- **Value:** Your Unity account email

#### Secret 3: UNITY_PASSWORD
- **Name:** `UNITY_PASSWORD`
- **Value:** Your Unity account password

### Step 3: Done! 🎉

Now whenever you push a version tag, GitHub will automatically:
1. Build the Windows executable
2. Zip it up
3. Create a GitHub Release
4. Upload the build

---

## How to Create a Release

### Method 1: Command Line
```bash
# Tag your current commit
git tag v0.1.0

# Push the tag
git push origin v0.1.0
```

### Method 2: GitHub Web UI
1. Go to: https://github.com/dc6g4xtrm4-stack/plunk-and-plunder/releases
2. Click **"Draft a new release"**
3. Click **"Choose a tag"** → Type new tag like `v0.1.0`
4. Click **"Create new tag on publish"**
5. Add release notes (optional)
6. Click **"Publish release"**

GitHub Actions will automatically:
- Build the game
- Attach `PlunkAndPlunder-Windows.zip` to the release

---

## Checking Build Status

1. Go to: https://github.com/dc6g4xtrm4-stack/plunk-and-plunder/actions
2. You'll see the build progress
3. Click on a workflow run to see logs
4. When complete, the release will appear at: `/releases`

---

## Manual Trigger (No Tag Required)

You can also trigger a build manually without creating a release:

1. Go to: https://github.com/dc6g4xtrm4-stack/plunk-and-plunder/actions
2. Select **"Build and Release"** workflow
3. Click **"Run workflow"** → **"Run workflow"**
4. Build will run and upload an artifact (expires after 7 days)

---

## Troubleshooting

### "Unity license not valid"
- Make sure you copied the **entire** .ulf file contents
- Check that UNITY_EMAIL and UNITY_PASSWORD match your Unity account

### "Build failed"
- Check the Actions logs for errors
- Make sure your project builds locally first
- Verify Unity version matches: **2022.3.21f1**

### "Out of disk space"
- The workflow includes a step to free disk space
- If still issues, contact GitHub support (runners have limited space)

---

## Where to Download Builds

**For your friend:**
- Go to: https://github.com/dc6g4xtrm4-stack/plunk-and-plunder/releases/latest
- Download `PlunkAndPlunder-Windows.zip`
- Extract and run `PlunkAndPlunder.exe`

---

## Cost

- ✅ **Free for public repos**
- ✅ GitHub provides 2000 build minutes/month for free
- ✅ Each build takes ~15-20 minutes

---

Ready to automate your releases! 🚀
