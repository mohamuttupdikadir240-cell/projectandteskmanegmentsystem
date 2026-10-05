@echo off
echo ========================================================
echo   Pushing RealEstate PMS Fixes to GitHub for Railway
echo ========================================================
cd /d "%~dp0"

echo [1/3] Staging changes...
git add .

echo [2/3] Committing changes...
git commit -m "Fix database connection, add SQLite fallback, and configure Railway deployment"

echo [3/3] Pushing to GitHub (main)...
git push origin main

echo.
echo ========================================================
echo   SUCCESS! Pushed to GitHub.
echo   Railway will now automatically rebuild and deploy!
echo   Watch your Railway dashboard:
echo   https://railway.com/project/22950c10-510a-4d25-acdb-52101742de13
echo ========================================================
echo.
pause
