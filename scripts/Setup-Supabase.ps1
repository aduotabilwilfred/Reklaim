<#
.SYNOPSIS
    Points the Reclaim API at a Supabase PostgreSQL database.

.DESCRIPTION
    Writes the Supabase connection string into dotnet user-secrets for the API
    project, then optionally applies EF Core migrations to that database.

    Accepts either a Supabase connection URI (postgresql://...) or an existing
    Npgsql keyword/value string, and normalises both to Npgsql form.

    The password is read as a SecureString and is never written to disk by this
    script, echoed to the console, or committed to git.

.PARAMETER ConnectionUri
    The connection URI from Supabase -> Project Settings -> Database ->
    Connection string, e.g.
    postgresql://postgres.abcdefg:pa%40ss@aws-0-eu-central-1.pooler.supabase.com:6543/postgres?sslmode=require
    If omitted, the script prompts for the individual parts.

.PARAMETER SkipMigrations
    Set the connection string only; do not run 'dotnet ef database update'.

.EXAMPLE
    .\scripts\Setup-Supabase.ps1

.EXAMPLE
    .\scripts\Setup-Supabase.ps1 -ConnectionUri "postgresql://..."

.NOTES
    Use the direct connection (port 5432) for migrations. The transaction pooler
    (port 6543) does not support the session-level state EF migrations need.
#>
[CmdletBinding()]
param(
    [string]$ConnectionUri,
    [switch]$SkipMigrations
)

$ErrorActionPreference = 'Stop'

function ConvertTo-NpgsqlConnectionString {
    # Not $Input: that name collides with PowerShell's automatic $input enumerator.
    param([Parameter(Mandatory)][string]$Value)

    $text = $Value.Trim()

    # Already a keyword/value string (e.g. "Host=...;Database=...").
    if ($text -match '^\s*[A-Za-z]+\s*=') {
        return $text
    }

    if ($text -notmatch '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "Unrecognised connection format. Expected a postgresql:// URI or a Host=... keyword string."
    }

    $uri = [Uri]$text

    if ([string]::IsNullOrWhiteSpace($uri.Host)) {
        throw 'Connection URI is missing a host.'
    }

    $userInfo = $uri.UserInfo
    $separator = $userInfo.IndexOf(':')
    if ($separator -lt 1) {
        throw 'Connection URI is missing a username or password (expected postgres://user:password@host).'
    }

    $username = [Uri]::UnescapeDataString($userInfo.Substring(0, $separator))
    $password = [Uri]::UnescapeDataString($userInfo.Substring($separator + 1))

    $database = [Uri]::UnescapeDataString($uri.AbsolutePath.TrimStart('/'))
    if ([string]::IsNullOrWhiteSpace($database)) {
        $database = 'postgres'
    }

    $port = $uri.Port
    if ($port -le 0) {
        $port = 5432
    }

    # Npgsql wraps values in single quotes so ; and spaces in the password are safe.
    $safePassword = "'" + $password.Replace("'", "''") + "'"

    return "Host=$($uri.Host);Port=$port;Database=$database;Username=$username;Password=$safePassword;SSL Mode=Require;Trust Server Certificate=true"
}

function Format-Redacted {
    param([Parameter(Mandatory)][string]$ConnectionString)
    return ($ConnectionString -replace '(?i)(Password\s*=\s*)(''[^'']*''|[^;]*)', '$1***')
}

# --- Resolve the API project -------------------------------------------------

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiDir = Get-ChildItem -LiteralPath $repoRoot -Directory |
    Where-Object { $_.Name -like '*.api' } |
    Select-Object -First 1

if (-not $apiDir) {
    throw "Could not find the API project folder under $repoRoot"
}

$apiPath = $apiDir.FullName
$csproj = Get-ChildItem -LiteralPath $apiPath -Filter '*.csproj' | Select-Object -First 1
if (-not $csproj) {
    throw "Could not find a .csproj file in $apiPath"
}

Write-Host "API project: $($csproj.Name)" -ForegroundColor Cyan

# --- Gather connection details -----------------------------------------------

if ([string]::IsNullOrWhiteSpace($ConnectionUri)) {
    Write-Host ''
    Write-Host 'Press Enter to accept each default.' -ForegroundColor Yellow

    $ref = Read-Host 'Project ref (the xxx in db.xxx.supabase.co)'
    $dbHost = Read-Host 'Host (direct: db.<ref>.supabase.co | pooler: aws-0-<region>.pooler.supabase.com)'
    $username = Read-Host 'Username (direct: postgres | pooler: postgres.<ref>)'
    $port = Read-Host 'Port (5432 = direct/migrations, 6543 = transaction pooler)' -DefaultValue '5432'
    $database = Read-Host 'Database' -DefaultValue 'postgres'
    $securePassword = Read-Host 'Password' -AsSecureString

    if ([string]::IsNullOrWhiteSpace($ref) -or [string]::IsNullOrWhiteSpace($dbHost) -or
        [string]::IsNullOrWhiteSpace($username) -or [string]::IsNullOrWhiteSpace($database)) {
        throw 'Project ref, host, username and database are all required.'
    }

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    try {
        $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }

    # Round-trip through a URI so percent-encoding is handled by the same code path.
    $encoded = [Uri]::EscapeDataString($plain)
    $ConnectionUri = "postgresql://$($username):$encoded@$($dbHost):$port/$database"
}

# --- Convert, redact, store --------------------------------------------------

$conn = ConvertTo-NpgsqlConnectionString -Input $ConnectionUri

Write-Host ''
Write-Host "Connection string: $(Format-Redacted -ConnectionString $conn)"

Push-Location $apiPath
try {
    dotnet user-secrets set 'ConnectionStrings:DefaultConnection' $conn | Out-Host

    if ($SkipMigrations) {
        Write-Host 'Skipping migrations (-SkipMigrations).' -ForegroundColor Yellow
    }
    else {
        Write-Host ''
        Write-Host 'Applying migrations to Supabase...' -ForegroundColor Cyan
        dotnet ef database update
        if ($LASTEXITCODE -ne 0) {
            throw 'dotnet ef database update failed. Check the connection details and try again.'
        }
        Write-Host 'Migrations applied.' -ForegroundColor Green
    }
}
finally {
    Pop-Location
}

Write-Host ''
Write-Host 'Done. The API is now pointed at Supabase.' -ForegroundColor Green
