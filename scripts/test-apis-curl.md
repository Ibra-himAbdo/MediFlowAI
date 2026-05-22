# MediFlowAI API — Manual curl test

Base URL: `http://localhost:5167`

Run the API first: `dotnet run --project src/MediFlowAI.Api`

Or run all tests: `powershell -File scripts/test-apis.ps1`

---

## 1. Specializations

```bash
curl -s http://localhost:5167/api/Specializations
curl -s http://localhost:5167/api/Specializations/{SPECIALIZATION_ID}
```

## 2. Doctors

```bash
curl -s http://localhost:5167/api/Doctors
curl -s http://localhost:5167/api/Doctors/{DOCTOR_ID}
curl -s "http://localhost:5167/api/Doctors/specialization/{SPECIALIZATION_ID}"
curl -s "http://localhost:5167/api/Doctors/top/{SPECIALIZATION_ID}?take=5"
curl -s "http://localhost:5167/api/Doctors/search?q=cardio"
```

## 3. Schedules

```bash
curl -s "http://localhost:5167/api/Schedules?doctorId={DOCTOR_ID}&from=2026-05-23&to=2026-06-06"
curl -s "http://localhost:5167/api/Schedules/available?doctorId={DOCTOR_ID}&date=2026-05-25"
curl -s http://localhost:5167/api/Schedules/{SCHEDULE_ID}/available
```

## 4. Appointments

```bash
curl -s -X POST http://localhost:5167/api/Appointments/book \
  -H "Content-Type: application/json" \
  -d "{\"patientId\":\"{PATIENT_ID}\",\"doctorScheduleId\":\"{SCHEDULE_ID}\"}"

curl -s http://localhost:5167/api/Appointments/{APPOINTMENT_ID}
curl -s http://localhost:5167/api/Appointments/patient/{PATIENT_ID}
curl -s http://localhost:5167/api/Appointments/doctor/{DOCTOR_ID}

curl -s -X POST http://localhost:5167/api/Appointments/{APPOINTMENT_ID}/complete
curl -s -X POST http://localhost:5167/api/Appointments/{APPOINTMENT_ID}/cancel
```

## 5. Reviews

```bash
curl -s -X POST http://localhost:5167/api/Reviews \
  -H "Content-Type: application/json" \
  -d "{\"appointmentId\":\"{APPOINTMENT_ID}\",\"rate\":5,\"comment\":\"Great doctor\"}"

curl -s http://localhost:5167/api/Reviews/doctor/{DOCTOR_ID}
curl -s http://localhost:5167/api/Reviews/doctor/{DOCTOR_ID}/average
```

## Happy-path order

1. Specializations → copy `id`
2. Doctors/specialization → copy `doctorId`
3. Schedules/available → copy slot `id`
4. Appointments/doctor → copy `patientId` from existing row, or use any seeded patient GUID
5. Appointments/book → copy `appointmentId`
6. Appointments/complete
7. Reviews POST
8. Reviews/doctor/average
