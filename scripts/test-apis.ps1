# MediFlowAI - Full API test (curl.exe)
$Base = "http://localhost:5167"
$Passed = 0
$Failed = 0

function Write-Step { param($msg) Write-Host "`n=== $msg ===" -ForegroundColor Cyan }
function Pass { param($name) $script:Passed++; Write-Host "PASS: $name" -ForegroundColor Green }
function Fail { param($name, $detail) $script:Failed++; Write-Host "FAIL: $name - $detail" -ForegroundColor Red }

function Curl-Json {
    param(
        [Parameter(Mandatory, Position = 0)][string]$Url,
        [string]$Method = "GET",
        [string]$Body = $null
    )
    $tempFile = $null
    $args = @("-s", "-w", "`n%{http_code}", "-X", $Method, $Url, "-H", "Accept: application/json")
    if ($Body) {
        $tempFile = [System.IO.Path]::GetTempFileName()
        [System.IO.File]::WriteAllText($tempFile, $Body, [System.Text.UTF8Encoding]::new($false))
        $args += @("-H", "Content-Type: application/json", "--data-binary", "@$tempFile")
    }
    try {
        $raw = & curl.exe @args
    }
    finally {
        if ($Body -and (Test-Path $tempFile)) { Remove-Item $tempFile -Force -ErrorAction SilentlyContinue }
    }
    $lines = $raw -split "`n"
    $code = [int]$lines[-1]
    $json = ($lines[0..($lines.Length - 2)] -join "`n")
    return @{ Code = $code; Body = $json; Json = if ($json) { $json | ConvertFrom-Json } else { $null } }
}

function Expect-Ok {
    param($name, $result, [int[]]$Allowed = @(200))
    if ($result.Code -in $Allowed) { Pass $name; return $true }
    Fail $name "HTTP $($result.Code) - $($result.Body)"
    return $false
}

Write-Host "MediFlowAI API Test Suite (curl)" -ForegroundColor Yellow
Write-Host "Base: $Base"

# 1. Specializations
Write-Step "GET /api/Specializations"
$r = Curl-Json "$Base/api/Specializations"
if (-not (Expect-Ok "GET /api/Specializations" $r)) { exit 1 }
$specId = $r.Json[0].id
Write-Host "  specId=$specId"

Write-Step "GET /api/Specializations/{id}"
$r = Curl-Json "$Base/api/Specializations/$specId"
Expect-Ok "GET /api/Specializations/{id}" $r | Out-Null

Write-Step "GET /api/Specializations/search?q=card"
$r = Curl-Json "$Base/api/Specializations/search?q=card"
if (-not (Expect-Ok "GET /api/Specializations/search" $r)) { exit 1 }

# 2. Patients
$demoPatientId = "019e513f-b4aa-7dc3-aa1d-2ed781164ae0"

Write-Step "GET /api/Patients"
$r = Curl-Json "$Base/api/Patients"
if (-not (Expect-Ok "GET /api/Patients" $r)) { exit 1 }

Write-Step "GET /api/Patients/{id} (demo)"
$r = Curl-Json "$Base/api/Patients/$demoPatientId"
if (-not (Expect-Ok "GET /api/Patients/{id}" $r)) { exit 1 }

Write-Step "GET /api/Patients/search?q=Demo"
$r = Curl-Json "$Base/api/Patients/search?q=Demo"
if (-not (Expect-Ok "GET /api/Patients/search" $r)) { exit 1 }

# 3. Doctors
Write-Step "GET /api/Doctors"
$r = Curl-Json "$Base/api/Doctors"
if (-not (Expect-Ok "GET /api/Doctors" $r)) { exit 1 }
$doctorId = $r.Json[0].id
Write-Host "  doctorId=$doctorId"

Write-Step "GET /api/Doctors/{id}"
$r = Curl-Json "$Base/api/Doctors/$doctorId"
Expect-Ok "GET /api/Doctors/{id}" $r | Out-Null

Write-Step "GET /api/Doctors/specialization/{id}"
$r = Curl-Json "$Base/api/Doctors/specialization/$specId"
Expect-Ok "GET /api/Doctors/specialization/{id}" $r | Out-Null

Write-Step "GET /api/Doctors/top/{id}?take=3"
$r = Curl-Json "$Base/api/Doctors/top/$specId`?take=3"
Expect-Ok "GET /api/Doctors/top/{id}" $r | Out-Null

Write-Step "GET /api/Doctors/search?q=cardio"
$r = Curl-Json "$Base/api/Doctors/search?q=cardio"
Expect-Ok "GET /api/Doctors/search" $r | Out-Null

Write-Step "GET /api/Doctors/search empty (expect 400)"
$r = Curl-Json "$Base/api/Doctors/search?q="
Expect-Ok "GET /api/Doctors/search empty" $r -Allowed @(400) | Out-Null

# 4. Schedules
$from = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd")
$to = (Get-Date).ToUniversalTime().AddDays(14).ToString("yyyy-MM-dd")

Write-Step "GET /api/Schedules?doctorId&from&to"
$r = Curl-Json "$Base/api/Schedules?doctorId=$doctorId&from=$from&to=$to"
Expect-Ok "GET /api/Schedules range" $r | Out-Null

$scheduleId = $null
Write-Step "GET /api/Schedules/available (find slot)"
for ($d = 1; $d -le 14; $d++) {
    $date = (Get-Date).ToUniversalTime().AddDays($d).ToString("yyyy-MM-dd")
    $r = Curl-Json "$Base/api/Schedules/available?doctorId=$doctorId&date=$date"
    if ($r.Code -eq 200 -and $r.Json.Count -gt 0) {
        $scheduleId = $r.Json[0].id
        Write-Host "  date=$date scheduleId=$scheduleId"
        Pass "GET /api/Schedules/available"
        break
    }
}
if (-not $scheduleId) {
    foreach ($doc in (Curl-Json "$Base/api/Doctors").Json) {
        for ($d = 1; $d -le 14; $d++) {
            $date = (Get-Date).ToUniversalTime().AddDays($d).ToString("yyyy-MM-dd")
            $r = Curl-Json "$Base/api/Schedules/available?doctorId=$($doc.id)&date=$date"
            if ($r.Code -eq 200 -and $r.Json.Count -gt 0) {
                $doctorId = $doc.id
                $scheduleId = $r.Json[0].id
                Write-Host "  fallback doctorId=$doctorId scheduleId=$scheduleId"
                Pass "GET /api/Schedules/available (fallback doctor)"
                break
            }
        }
        if ($scheduleId) { break }
    }
}
if (-not $scheduleId) { Fail "GET /api/Schedules/available" "No slot found" }

Write-Step "GET /api/Schedules/{id}/available"
$r = Curl-Json "$Base/api/Schedules/$scheduleId/available"
Expect-Ok "GET /api/Schedules/{id}/available" $r | Out-Null

# 5. Resolve patient (prefer demo AI patient)
$patientId = $null
Write-Step "GET /api/Appointments/doctor/{id}"
$r = Curl-Json "$Base/api/Appointments/doctor/$doctorId"
if ($r.Code -eq 200 -and $r.Json.Count -gt 0) {
    $patientId = $r.Json[0].patientId
    Pass "GET /api/Appointments/doctor/{id}"
}
else {
    $docs = (Curl-Json "$Base/api/Doctors").Json
    foreach ($doc in $docs) {
        $r = Curl-Json "$Base/api/Appointments/doctor/$($doc.id)"
        if ($r.Code -eq 200 -and $r.Json.Count -gt 0) {
            $patientId = $r.Json[0].patientId
            Pass "GET /api/Appointments/doctor/{id} (any doctor)"
            break
        }
    }
}
if (-not $patientId) {
    $patientId = $demoPatientId
    Write-Host "  patientId=$patientId (demo patient)"
    Pass "Resolve patientId (demo fallback)"
}
else { Write-Host "  patientId=$patientId" }

# 6. Appointments
Write-Step "POST /api/Appointments/book"
$bookBody = "{`"patientId`":`"$patientId`",`"doctorScheduleId`":`"$scheduleId`"}"
$r = Curl-Json "$Base/api/Appointments/book" -Method POST -Body $bookBody
if ($r.Code -eq 200 -and $r.Json.success) {
    $appointmentId = $r.Json.appointmentId
    Pass "POST /api/Appointments/book"
    Write-Host "  appointmentId=$appointmentId"
}
else { Fail "POST /api/Appointments/book" $r.Body }

Write-Step "GET /api/Appointments/{id}"
$r = Curl-Json "$Base/api/Appointments/$appointmentId"
Expect-Ok "GET /api/Appointments/{id}" $r | Out-Null

Write-Step "GET /api/Appointments/patient/{id}"
$r = Curl-Json "$Base/api/Appointments/patient/$patientId"
Expect-Ok "GET /api/Appointments/patient/{id}" $r | Out-Null

Write-Step "GET /api/Appointments/doctor/{id}"
$r = Curl-Json "$Base/api/Appointments/doctor/$doctorId"
Expect-Ok "GET /api/Appointments/doctor/{id}" $r | Out-Null

Write-Step "POST /api/Appointments/{id}/complete"
$r = Curl-Json "$Base/api/Appointments/$appointmentId/complete" -Method POST
Expect-Ok "POST /api/Appointments/complete" $r -Allowed @(200,204) | Out-Null

Write-Step "GET /api/Appointments/{id} (Completed)"
$r = Curl-Json "$Base/api/Appointments/$appointmentId"
if ($r.Json.status -eq "Completed") { Pass "GET appointment status Completed" } else { Fail "GET appointment status" $r.Json.status }

# 7. Reviews
Write-Step "POST /api/Reviews"
$reviewBody = "{`"appointmentId`":`"$appointmentId`",`"rate`":5,`"comment`":`"API curl test`"}"
$r = Curl-Json "$Base/api/Reviews" -Method POST -Body $reviewBody
Expect-Ok "POST /api/Reviews" $r -Allowed @(200,204) | Out-Null

Write-Step "GET /api/Reviews/doctor/{id}"
$r = Curl-Json "$Base/api/Reviews/doctor/$doctorId"
Expect-Ok "GET /api/Reviews/doctor/{id}" $r | Out-Null

Write-Step "GET /api/Reviews/doctor/{id}/average"
$r = Curl-Json "$Base/api/Reviews/doctor/$doctorId/average"
Expect-Ok "GET /api/Reviews/doctor/average" $r | Out-Null
Write-Host "  averageRate=$($r.Body)"

Write-Step "POST /api/Appointments/book duplicate (expect 400)"
$r = Curl-Json "$Base/api/Appointments/book" -Method POST -Body $bookBody
Expect-Ok "POST book duplicate slot" $r -Allowed @(400) | Out-Null

Write-Host "`n========================================" -ForegroundColor Yellow
Write-Host "PASSED: $Passed" -ForegroundColor Green
Write-Host "FAILED: $Failed" -ForegroundColor $(if ($Failed -gt 0) { "Red" } else { "Green" })
Write-Host "========================================"
if ($Failed -gt 0) { exit 1 }
