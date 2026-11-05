# Teacher requirements audit — C# implementation

Application audit completed on **27 October 2026**. The original assignment and delivery obligations were rechecked on **5 November 2026**. This audit re-read the original `C:\Users\muhammed\Desktop\hospital-appointment-system\docs\ProjeOdevi_Son.docx` using read-only Word XML extraction. Main-body paragraph locators P1–P50 include blank paragraphs. SHA-256 remains `E636C9E18DC27A995A69E7C59800C3CB7506FB17EDC0884F5D563D6F67593226`. No Java build/application/tests were run or reference files changed.

The written assignment permits any platform at P4. **The professor's later C# instruction, relayed by the owner, governs this submission.** This is a C#/.NET application, not a Java submission. T01–T13 below are the approved requirements inventory derived from the source paragraphs, not a claim that the DOCX itself numbers thirteen bullets.

## Application coverage

| Requirement / original source | Implemented evidence | Executed C# evidence |
| --- | --- | --- |
| T01 — OOP Hospital/Clinic Appointment System; P4 | Four production projects; MVC/Razor web application; real domain and persisted appointments. | Full 197-test suite; published Release manager → patient → staff → patient rehearsal. |
| T02 — Single-site and multiple branches; P4 | Facility with Hospital/Clinic type; first Branch created atomically; additional branches, branch departments/services and schedules. | ManagementIntegrationTests.FacilityCreationGrantsOwnershipAndSupportsSingleAndMultipleBranches; seeded clinic/hospital and manager form workflow. |
| T03 — Concrete classes, inheritance, polymorphism, encapsulation; P4 | Abstract StaffMember; Doctor/HealthcareStaff overrides; private state and named behavior; shared booking contract and immutable TimeInterval. | BookingDomainTests.BothStaffTypesImplementTheRealSharedProfessionalContract; HealthcareStaffAndTphMappingUseTheSameBookingOperation; domain atomic-mutation tests. |
| T04 — Authorized institution definitions; P12 | FacilityManagementService creates/edits institutions, branches and creator grants; ManagementScope checks role/grant and child ownership. | EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly; ForgedChildResourcesAreRejectedEvenWhenManagerOwnsTheUrlFacility; real HTTP role/owner restrictions. |
| T05 — Opening hours and departments; P13 | Branch department forms; multiple weekly opening periods, lunch gaps, closed weekdays; future shift/reservation preservation. | ManagementScheduleTests.HoursProtectFutureUnbookedShiftsAndValidatePeriodsBeforeReplacement; ClosingUnreservedDayPreservesOtherWeekdays; real hours/department POSTs. |
| T06 — Services with duration and fees; P14 | CatalogManagementService; positive 1–1439-minute durations, decimal TRY fees stored as integer kuruş; edit/deactivate/reactivate and immutable booked terms. | InvalidServiceTermsRollbackWithoutRounding; CatalogAndNameEditsKeepBookedSnapshotsAndExactMoney; FinalReviewIntegrationTests.ExcessFractionalDigitsCannotRoundIntoAnAcceptedFee. |
| T07 — Register doctors and healthcare personnel; P16 | Distinct forms create real Doctor and HealthcareStaff instances; optional new STAFF Identity account in the same transaction. | DoctorAndHealthcareStaffRemainPolymorphicAndOfferingAssignmentIsIdempotent; NewStaffLoginHasOnlyStaffRoleAndUsesIdentityHashing; profile failure rollback and real forms. |
| T08 — Doctor specialty and provided services; P17 | Required primary Specialty; StaffOffering explicitly associates qualified employee/service and derives branch; specialty alone grants no offering. | Specialty edits and shared professional descriptions; missing/inactive branch assignment rejection; offering reuse/reactivation and full form workflow. |
| T09 — Availability definition and booking restriction; P18 | ScheduleManagementService manages dated shifts; full duration must fit both opening and matching branch availability; cross-branch shift overlap prohibited. | AvailabilityMustFitTheEntireOpeningUnionIncludingLunchGap; ShrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations; full-duration and boundary booking tests. |
| T10 — Choose suitable worker and procedure; P20 | Patient institution/branch/service/employee/date selectors and server-calculated starts; both staff subtypes book through AppointmentService. | RealPatientBookingStaffApprovalAndPatientConfirmationIgnoreForgedTerms; new-manager-configuration workflow; keyboard selection rehearsal. |
| T11 — Conflict detection; P21 | Half-open employee/patient interval conflicts, spanning branches; Pending/Approved reserve; SQLite writer acquired before authoritative reads. | BookingConcurrencyTests; ten ManagementConcurrencyTests cases; CrossProcessBookingTests with two Kestrel processes and one SQLite file. |
| T12 — Save operation, duration, fee, employee and date/time; P22 | Appointment saves offering/patient links, UTC instants, service/staff names, duration, fee/currency snapshots and audit/status. | Exact integer/UTC round trips, catalog snapshot tests, restrictive history deletion, separate-host persistence and published-process restart. |
| T13 — Approval mechanism; P23 | Pending → Approved/Rejected, exactly once, by assigned staff or granted manager; actor/time recorded; patient sees Turkish result. | DecisionsAreOneWayAndInvalidAuditInputIsAtomic; competing decision test; new doctor's real Identity login/approval and patient Onaylandı confirmation after restart. |

Result: **all thirteen application requirements have implemented, executable evidence**. No missing mandatory feature was found. The final review fixed a fee-input precision defect and improved UI/accessibility rather than adding unrelated scope. General examination/radiology/laboratory are source examples, not a mandated fixed catalog. Facility/branch names and professional descriptions are current linked labels; booked service/staff names and terms are the specified snapshots.

P25 recommends at least three role groups. PATIENT, STAFF and MANAGER are implemented and additionally mandatory through owner instructions. Role routes supplement service-level ownership; hidden controls do not authorize actions.

## Administrative and delivery obligations

| Source | Status / owner action |
| --- | --- |
| P10/P40 — Individual or at most two contributors | Actual group/contributor declaration remains for the owner to confirm. |
| P41/P43 — GitHub and SABİS | Share the [C# repository](https://github.com/tk612536-ops/hospital-appointment-system-csharp) and confirm reviewer access. See the [delivery checklist](RUN_ON_WINDOWS.md#course-delivery-checklist) for the source ZIP. Upload and receipt remain owner actions. |
| P42 — At least ten commits on different days | Verify the count and dates against `main` on GitHub. The source ZIP omits Git history. |
| P43 — Upload before deadline | Owner-supplied deadline: **15 November 2026 at 23:59 Türkiye time**. The DOCX does not specify an archive format. Follow any additional course file instructions and verify the receipt. |
| P44/P45/P49 — Present and attend | Owner reports presentations begin **17 November 2026**. Check the individual date/time and attend. The assignment states absence forfeits the project points. |
| P50 — Upload before participating | Verify submission receipt before presenting. |
| P46/P47 — Selection recorded by 13 October 2026 | The date precedes this audit. Selection/extension status remains unknown; no compliance or late permission is assumed. |

P44 mentions the last four weeks; P48 mentions week seven. Follow the teacher's announcement rather than inventing dates. Option B's SCI/SCIE article, three foundational sources and six-page paper requirements do not apply to this Option A software project.

## Evidence and deliberate boundaries

[Architecture review](ARCHITECTURE_REVIEW.md), [SQLite protocol](CONCURRENCY.md), [test parity](TEST_PARITY.md), [Stage 4 verification](STAGE4_VERIFICATION.md), [Turkish presentation guide](PRESENTATION_GUIDE_TR.md) and [Windows delivery](RUN_ON_WINDOWS.md) provide the matching implementation details.

Payments, medical records, cancellation/rescheduling/expiry, travel/room scheduling, overnight/holiday calendars, public signup/recovery and grant administration are excluded by the approved scope. Their absence is not a missing source requirement. Automated tests and this targeted review do not establish production hospital capacity, independent penetration testing or administrative course compliance.
