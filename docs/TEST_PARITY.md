# Java → C# test parity and verification plan

Status: Stages 1–4 verified, final review on **27 October 2026**. **197 C# tests passed (31 domain, 26 application, 140 integration), zero failures/skips.** All 191 accepted Stage 1–3 cases remain unchanged in Stage 4; six new meaningful HTTP cases cover the final review. Every historical execution row below has an implemented equivalent/adaptation/provider replacement. This is not a claim that 118 unchanged Java tests ran in C#, that counts are interchangeable, or that finite tests prove every possible deployment correct.

**Day 4 update, 29 October 2026:** the full suite now passes **200 cases (31 domain, 26 application, 143 integration)**, zero failures/skips. TRX name comparison confirms all 197 Stage 4 cases remain. `AppointmentRollbackTests.BookingFailureAfterInsertRollsBackAndReleasesBothPatientAndEmployee` and `DecisionFailureAfterUpdateRestoresPendingAuditAndReservation` (Approved/Rejected) extend the earlier low-level disposal test by executing the actual application service, saving real SQLite changes, faulting immediately before commit and checking persisted rollback/cleanup/fresh retry. No production or schema change was necessary. The historical Java mapping/counts below are unchanged; see [DAY4_VERIFICATION.md](DAY4_VERIFICATION.md).

**Day 5 update, 30 October 2026:** **202 cases pass (31 domain, 26 application, 145 integration)**, zero failures/skips, with all 200 Day 4 case names retained. `DemoLifecycleTests.FreshDemoSeedsTomorrowInIstanbulWhenUtcDateIsStillYesterday` checks the complete fourteen-date seed horizon when local/UTC dates differ. `AgedDemoRestartPreservesHistoryAndManagerCanExtendWithoutReset` advances the clock beyond all seed dates, restarts the same SQLite file, compares saved history/availability, rejects an unavailable future booking, and uses real manager/patient/staff forms to extend and approve a new booking. These extend the earlier same-clock restart cases; no production/schema change or new Java mapping is involved. See [DAY5_VERIFICATION.md](DAY5_VERIFICATION.md).

## Reference evidence and honest baseline

Read-only source: `C:\Users\muhammed\Desktop\hospital-appointment-system\src\test\java\com\example\clinic`. Existing reports: that project's `target/surefire-reports/TEST-*.xml`. The Java verification guide records 118 tests with zero failures/errors/skips. Stage 0 read the reports; it did not rerun Java tests or write in the reference project.

| Java suite | Historical executed cases | Main C# destination |
| --- | ---: | --- |
| UserAccountTest | 11 | Identity/access application and integration tests |
| AppointmentDomainTest | 11 | Domain tests; facility grant authorization moves to application/integration |
| ConfigurationDomainTest | 5 | Domain tests |
| TimeIntervalTest | 7 | Domain tests |
| UserAccountRepositoryTest | 2 | Identity/SQLite integration |
| BookingIntegrationTest | 27 | Booking/HTTP/SQLite integration |
| DemoDataIntegrationTest | 2 | Demo/SQLite integration |
| FinalReviewIntegrationTest | 10 | HTTP/domain/SQLite regression integration |
| ManagementIntegrationTest | 28 | Management/HTTP/SQLite integration |
| AuthenticationIntegrationTest | 13 | Real Identity/MVC integration |
| NonDemoProfileIntegrationTest | 2 | Non-demo host/time configuration integration |
| **Total** | **118** | **See per-row implemented/adapted coverage and provider-specific replacements** |

Retain each behavioral scenario, including every parameterized row. Do not chase an identical test count: one scenario can require multiple C# tests or a provider-specific replacement. Completion means the matrix is resolved with actual C# file/test identifiers, executed evidence and reasons for changes. Do not mark a scenario covered because its proposed name appears here.

## Equivalent, adapted and replaced

- **Equivalent**: preserve the same business invariant and inputs/boundaries, translated into C# conventions.
- **Adapted**: preserve the user/security intent while using Identity cookies, Turkish rendering, UTC storage, TPH mapping or SQLite semantics.
- **Replaced**: remove a Java/H2-specific mechanism assertion and supply direct evidence for the approved C# mechanism. Explain the replacement rather than silently omitting coverage.

Identity replaces custom UserAccount constructors/BCrypt-prefix checks. Use UserManager/SignInManager, hashed-password verification, role/profile composition and normalization/length regression tests. A caller never supplies a stored password hash to account creation. Identity's username alphabet and invariant normalization differ from Java's lowercasing; malformed or unsupported input must be an ordinary failed login, never an exception/500. Preserve trimming and case-insensitive ASCII login behavior without imitating Java Unicode expansion internals.

H2 joined-table/SELECT FOR UPDATE/lock-timeout assertions become TPH materialization, actual immediate-transaction acquisition/enlistment, fresh reads after a winning commit and SQLite bounded-busy rollback tests. Database/unknown routes remain unavailable; an ordinary unmapped MVC route may return 404 rather than the Java deny-all 403.

ASP.NET Core antiforgery normally rejects invalid tokens with 400 rather than Java's 403. Test rejection and unchanged database/authentication state. Real login/logout tests use cookies and antiforgery tokens; logout clears browser authentication rather than relying on Java server-session semantics. Test role/object denials separately. Rendered status text is Turkish; assertions use the underlying status plus the approved Turkish label, not literal Java English page text.

## C# testing layers and acceptance

Domain.Tests has no EF/web host: invariant atomicity, mixed staff polymorphism, exact fees/durations, snapshot construction, interval coverage and legal decisions. Application.Tests uses fixed time and small focused doubles for use-case policy. IntegrationTests applies real migrations to isolated SQLite files and uses WebApplicationFactory with actual Identity forms/cookies/antiforgery where HTTP behavior matters.

Never use EF InMemory or mocked database locks to prove booking correctness. Each concurrency competitor gets an independent scope/context/connection to the same temporary file. Synchronize before acquiring the writer position; verify committed rows and intervals. Set bounded waits; avoid sleeps and shared contexts. See [CONCURRENCY.md](CONCURRENCY.md) for the full protocol and scenarios.

Additional C# acceptance cases beyond the Java baseline:

- Exact decimal ↔ integer minor-unit round trips, bounds/fractional-kuruş rejection, UTC-tick comparisons/kind restoration, Turkish decimal input and weekday/timezone conversion.
- Actual TPH discriminator/subtype constraints, migrated schema/FKs, identity/profile/grant relationships, query translation and restrictive history deletion.
- Immediate writer acquisition before mutable catalog/schedule reads and EF participation in that transaction; no stale tracked state carried from preflight.
- Busy timeout/cancellation leaves no partial domain or optional Identity account/role writes; a fresh retry succeeds after the holder releases.
- Cross-process conflict test against the same local SQLite file; correctness must not depend on a process-local mutex.
- Five-failure/five-minute Identity lockout and non-demo absence of fixtures/logins/credential banners.
- Persistent restart using the published app; marker survives renamed fixtures; appointments are neither moved nor duplicated; initial fixtures use future dates.
- Browser rehearsal of manager-created service/staff/shift → patient booking → assigned staff decision → patient result, keyboard/mobile layout and local assets.

Each stage records relevant build/test commands, date, versions, outcomes/counts and limitations. Final verification includes locked restore, full suite, dependency review, Release publish and offline-assets/restart checks. Do not imply Stage 0 ran these future commands.

## Per-execution migration matrix

The following rows inventory each execution in the existing Java XML reports, including indexed parameter cases. Preserve source row values from the Java test source when implementing; index numbers are report locators, not independent requirement IDs. Unverified targets remain proposals. Verified target names refer to classes in the current test projects; parameterized Java cases can map to one C# theory or a combined test without implying identical counts.

### UserAccountTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J001 | `rejectsAnUnencodedPassword` | `FoundationIntegrationTests.PasswordsAreHashedAndSeederNeverReplacesStoredCredentials` | Replaced | Stage 1 verified (adapted where required) |
| J002 | `rolesAreComposedAndDefensivelyCopied` | `AccountProfileTests.RolesAreComposedAndDefensivelyCopied` | Adapted | Stage 1 verified (adapted where required) |
| J003 | `rejectsNullRoleElements` | `AccountProfileTests.EmptyAndUnknownRolesAreRejected` | Replaced | Stage 1 verified; non-nullable enum replaces nullable role elements |
| J004 | `rejectsAccountsWithoutRoles` | `FoundationIntegrationTests.AnAccountWithoutAssignedRolesCannotLogin` | Adapted | Stage 1 verified (adapted where required) |
| J005 | `rejectsBlankUsername(String)[1]` | `AccountServiceTests.InvalidLoginShapeNeverReachesIdentity` | Adapted | Stage 1 verified (adapted where required) |
| J006 | `rejectsBlankUsername(String)[2]` | `AccountServiceTests.InvalidLoginShapeNeverReachesIdentity` | Adapted | Stage 1 verified (adapted where required) |
| J007 | `rejectsBlankUsername(String)[3]` | `AccountServiceTests.InvalidLoginShapeNeverReachesIdentity` | Adapted | Stage 1 verified (adapted where required) |
| J008 | `rejectsBlankUsername(String)[4]` | `AccountServiceTests.InvalidLoginShapeNeverReachesIdentity` | Adapted | Stage 1 verified (adapted where required) |
| J009 | `normalizationCannotExpandAUsernamePastItsDatabaseLimit` | `FoundationIntegrationTests.WeakPasswordsAndOversizedUsernamesAreRejected` | Replaced | Stage 1 verified (adapted where required) |
| J010 | `rejectsAnOverlongUsernameBeforePersistence` | `FoundationIntegrationTests.WeakPasswordsAndOversizedUsernamesAreRejected` | Adapted | Stage 1 verified (adapted where required) |
| J011 | `normalizesLoginIdentityWithoutChangingDisplayName` | `FoundationIntegrationTests.LoginAcceptsTrimmedCaseInsensitiveUsernameAndRejectsExternalReturnUrl` | Adapted | Stage 1 verified (adapted where required) |

### AppointmentDomainTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J012 | `pendingApprovalAndRejectionProtectTheirTransitionsAndAudit` | `BookingDomainTests.DecisionsAreOneWayAndInvalidAuditInputIsAtomic` | Equivalent | Stage 2 verified (adapted where required) |
| J013 | `onlyManagersCanReceiveFacilityGrants` | `ManagementIntegrationTests.EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly + FacilityCreationGrantsOwnershipAndSupportsSingleAndMultipleBranches` | Adapted | Stage 3 verified (adapted where required) |
| J014 | `invalidDecisionInputCannotPartiallyChangeAuditState` | `BookingDomainTests.DecisionsAreOneWayAndInvalidAuditInputIsAtomic` | Equivalent | Stage 2 verified (adapted where required) |
| J015 | `offeringCannotJoinDifferentEmployingFacilities` | `BookingDomainTests.OfferingAndAvailabilityCannotCrossEmployingFacilities` | Equivalent | Stage 2 verified (adapted where required) |
| J016 | `differentStaffTypesHonorTheSameContractWithoutTypeSwitches` | `BookingDomainTests.BothStaffTypesImplementTheRealSharedProfessionalContract` | Equivalent | Stage 2 verified (adapted where required) |
| J017 | `appointmentMustMatchServerSideDuration` | `BookingDomainTests.StateMethodsDoNotReassignOwnershipAndDeactivationRetainsHistory` | Equivalent | Stage 2 verified (adapted where required) |
| J018 | `catalogChangesCannotRewriteBookedSnapshots` | `BookingDomainTests.BookedSnapshotsSurviveCatalogAndEmployeeEdits` | Equivalent | Stage 2 verified (adapted where required) |
| J019 | `rejectsInvalidServiceDurations(int)[1]` | `BookingDomainTests.InvalidServiceDurationsLeaveTermsUnchanged` | Equivalent | Stage 2 verified (adapted where required) |
| J020 | `rejectsInvalidServiceDurations(int)[2]` | `BookingDomainTests.InvalidServiceDurationsLeaveTermsUnchanged` | Equivalent | Stage 2 verified (adapted where required) |
| J021 | `rejectsInvalidServiceDurations(int)[3]` | `BookingDomainTests.InvalidServiceDurationsLeaveTermsUnchanged` | Equivalent | Stage 2 verified (adapted where required) |
| J022 | `rejectsNegativeOrFractionalCentFees` | `BookingDomainTests.FeesRequireExactNonnegativeMinorUnits` | Equivalent | Stage 2 verified (adapted where required) |

### ConfigurationDomainTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J023 | `professionalEditsRemainPolymorphicAndDoNotChangeEmployeeType` | `ManagementIntegrationTests.DoctorAndHealthcareStaffRemainPolymorphicAndOfferingAssignmentIsIdempotent` | Adapted | Stage 3 verified (adapted where required) |
| J024 | `invalidFacilityTypeCannotPartiallyRenameIt` | `BookingDomainTests.StateMethodsDoNotReassignOwnershipAndDeactivationRetainsHistory` | Equivalent | Stage 2 verified (adapted where required) |
| J025 | `renamingProtectsOwnershipAndValidatesState` | `ManagementDomainTests.InvalidNamedEditsLeaveNamesAndOwnershipUnchanged + ManagementIntegrationTests.ForgedChildResourcesAreRejectedEvenWhenManagerOwnsTheUrlFacility` | Equivalent | Stage 3 verified (adapted where required) |
| J026 | `reschedulingPreservesResourceLinksAndRejectsOvernightIntervals` | `ManagementDomainTests.ReschedulingPreservesTheEmployeeAndBranchAndRejectsNullAtomically + ManagementScheduleTests.NonPositiveOrOvernightIntervalsAreRejected` | Adapted | Stage 3 verified (adapted where required) |
| J027 | `activeStatesChangeEligibilityWithoutRemovingAssociations` | `BookingDomainTests.StateMethodsDoNotReassignOwnershipAndDeactivationRetainsHistory` | Equivalent | Stage 2 verified (adapted where required) |

### TimeIntervalTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J028 | `rejectsZeroAndNegativeLength` | `BookingDomainTests.InvalidIntervalsAndNonUtcInstantsAreRejected` | Equivalent | Stage 2 verified (adapted where required) |
| J029 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[1]` | `BookingDomainTests.OverlapIsSymmetricAndEndExclusive` | Equivalent | Stage 2 verified (adapted where required) |
| J030 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[2]` | `BookingDomainTests.OverlapIsSymmetricAndEndExclusive` | Equivalent | Stage 2 verified (adapted where required) |
| J031 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[3]` | `BookingDomainTests.OverlapIsSymmetricAndEndExclusive` | Equivalent | Stage 2 verified (adapted where required) |
| J032 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[4]` | `BookingDomainTests.OverlapIsSymmetricAndEndExclusive` | Equivalent | Stage 2 verified (adapted where required) |
| J033 | `continuousCoverageMergesAdjacentButNeverBridgesGaps` | `BookingDomainTests.CoverageMergesAdjacencyButNeverBridgesPositiveGaps` | Equivalent | Stage 2 verified (adapted where required) |
| J034 | `containmentRequiresTheWholeIntervalIncludingTheEndBoundary` | `BookingDomainTests.CoverageMergesAdjacencyButNeverBridgesPositiveGaps` | Equivalent | Stage 2 verified (adapted where required) |

### UserAccountRepositoryTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J035 | `databaseRejectsDuplicateNormalizedUsernames` | `FoundationIntegrationTests.DatabaseAlsoRejectsDuplicateNormalizedUsername` | Adapted | Stage 1 verified (adapted where required) |
| J036 | `persistsAndReloadsAccountWithMultipleRoles` | `FoundationIntegrationTests.RolesComposeAndNormalizedUsernamesRemainUnique` | Adapted | Stage 1 verified (adapted where required) |

### BookingIntegrationTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J037 | `simultaneousBackToBackRequestsBothCommit` | `BookingConcurrencyTests.ConcurrentAdjacentBookingsBothCommit` | Adapted | Stage 2 verified (adapted where required) |
| J038 | `branchLunchGapAndClosingBoundaryRejectPartialCoverage(String)[1]` | `BookingIntegrationTests.BranchLunchAndClosingGapsRejectTheFullInterval` | Equivalent | Stage 2 verified (adapted where required) |
| J039 | `branchLunchGapAndClosingBoundaryRejectPartialCoverage(String)[2]` | `BookingIntegrationTests.BranchLunchAndClosingGapsRejectTheFullInterval` | Equivalent | Stage 2 verified (adapted where required) |
| J040 | `branchLunchGapAndClosingBoundaryRejectPartialCoverage(String)[3]` | `BookingIntegrationTests.BranchLunchAndClosingGapsRejectTheFullInterval` | Equivalent | Stage 2 verified (adapted where required) |
| J041 | `staleCatalogVersionRejectedAndExistingSnapshotsRemainStable` | `BookingIntegrationTests.SnapshotTermsAndNamesSurviveEditsAndStaleFormIsRejected` | Equivalent | Stage 2 verified (adapted where required) |
| J042 | `candidatesUseTheSameFullDurationAndPatientConflictRules` | `BookingIntegrationTests.CandidatesReuseFullDurationAndPatientConflictRules` | Equivalent | Stage 2 verified (adapted where required) |
| J043 | `databaseLockTimeoutRollsBackWithoutPartialReservation` | `BookingConcurrencyTests.BusyTimeoutIsBoundedLeavesNoReservationAndAllowsFreshRetry` | Replaced | Stage 2 verified (adapted where required) |
| J044 | `adjacentAvailabilityWindowsJoinButPositiveGapsDoNot` | `BookingIntegrationTests.AdjacentWindowsMergeAndPositiveAvailabilityGapsDoNot` | Equivalent | Stage 2 verified (adapted where required) |
| J045 | `patientsStaffAndManagersAreRestrictedByObjectOwnership` | `BookingIntegrationTests.OwnershipFacilityGrantsAndDecisionTransitionsAreEnforcedInServices` | Equivalent | Stage 2 verified (adapted where required) |
| J046 | `sameEmployeeConflictQueryNeverFiltersByBranch` | `BookingConcurrencyTests.SameStaffAcrossBranchesIsOneGlobalResource` | Equivalent | Stage 2 verified (adapted where required) |
| J047 | `simultaneousSamePatientDifferentStaffAndBranchesHaveOneWinner` | `BookingConcurrencyTests.ConflictingIndependentBookingsHaveExactlyOneWinner` | Adapted | Stage 2 verified (adapted where required) |
| J048 | `wholeDurationEndingAtAvailabilityBoundaryIsAcceptedAndPersisted` | `BookingIntegrationTests.EntireDurationAtBoundaryBooksWithExactTermsAndUtcPersistence` | Equivalent | Stage 2 verified (adapted where required) |
| J049 | `samePatientCannotOverlapDifferentStaffOrBranches` | `BookingIntegrationTests.PatientConflictsAcrossEmployeesAndBranchesAndAdjacencyWorks` | Equivalent | Stage 2 verified (adapted where required) |
| J050 | `pendingAndApprovedBlockButRejectedReleasesTime` | `BookingIntegrationTests.PendingApprovedReserveAndRejectedReleases` | Adapted | Stage 2 verified (adapted where required) |
| J051 | `backToBackAppointmentsCanBothReserve` | `BookingIntegrationTests.PatientConflictsAcrossEmployeesAndBranchesAndAdjacencyWorks` | Equivalent | Stage 2 verified (adapted where required) |
| J052 | `malformedOrConflictingBookingProducesFeedbackWithoutSaving` | `BookingIntegrationTests.EmptyCatalogAndMalformedPostsRemainUsableAndSafe + BookingPersistenceTests.BusyHttpResponseIs503AndPreservesSelectionWithoutSaving` | Adapted | Stage 2 verified (adapted where required) |
| J053 | `simultaneousSameStaffRequestsWithNoExistingAppointmentsHaveOneWinner` | `BookingConcurrencyTests.ConflictingIndependentBookingsHaveExactlyOneWinner` | Adapted | Stage 2 verified (adapted where required) |
| J054 | `pastOffGridAndSecondPrecisionStartsAreRejected` | `AvailabilityRulesTests.CurrentInstantAndPastStartsAreRejected + OffGridAndSubMinuteStartsAreRejected` | Equivalent | Stage 2 verified (adapted where required) |
| J055 | `healthcareStaffBooksThroughTheSharedFlowAndJoinedMapping` | `BookingIntegrationTests.HealthcareStaffAndTphMappingUseTheSameBookingOperation` | Adapted | Stage 2 verified (adapted where required) |
| J056 | `freeStartIsInsufficientWithoutWholeDurationCoverage(String, String)[1]` | `BookingIntegrationTests.PartialDurationWrongBranchHoursAndOffGridAreRejected` | Equivalent | Stage 2 verified (adapted where required) |
| J057 | `freeStartIsInsufficientWithoutWholeDurationCoverage(String, String)[2]` | `BookingIntegrationTests.PartialDurationWrongBranchHoursAndOffGridAreRejected` | Equivalent | Stage 2 verified (adapted where required) |
| J058 | `freeStartIsInsufficientWithoutWholeDurationCoverage(String, String)[3]` | `BookingIntegrationTests.PartialDurationWrongBranchHoursAndOffGridAreRejected` | Equivalent | Stage 2 verified (adapted where required) |
| J059 | `simultaneousDecisionsCanTransitionPendingOnlyOnce` | `BookingConcurrencyTests.CompetingApprovalAndRejectionTransitionExactlyOnce` | Adapted | Stage 2 verified (adapted where required) |
| J060 | `joinedStaffMappingActuallyLocksTheSharedStaffTable` | `BookingConcurrencyTests.ImmediateScopeReservesWriterBeforeAnyDomainReadAndEnlistsEf` | Replaced | Stage 2 verified (adapted where required) |
| J061 | `renderedBookingListDetailsAndDecisionFormsWorkWithoutTrustedClientTerms` | `BookingIntegrationTests.RealPatientBookingStaffApprovalAndPatientConfirmationIgnoreForgedTerms` | Adapted | Stage 2 verified (adapted where required) |
| J062 | `workerListsRespectAssignedEmployeeAndFacilityGrants` | `BookingIntegrationTests.OwnershipFacilityGrantsAndDecisionTransitionsAreEnforcedInServices` | Equivalent | Stage 2 verified (adapted where required) |
| J063 | `simultaneousCrossBranchStaffRequestsHaveOneWinner` | `BookingConcurrencyTests.SameStaffAcrossBranchesIsOneGlobalResource` | Adapted | Stage 2 verified (adapted where required) |

### DemoDataIntegrationTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J064 | `demoAppointmentsUseFutureDatesAndSeedingNeverMovesOrDuplicatesThem` | `BookingPersistenceTests.RenamedSeedAndDecidedAppointmentSurviveRestartWithoutMovingDates` | Equivalent | Stage 2 verified (adapted where required) |
| J065 | `bothPersistedSubtypesWorkThroughTheAbstractRepositoryContract` | `BookingIntegrationTests.HealthcareStaffAndTphMappingUseTheSameBookingOperation` | Equivalent | Stage 2 verified (adapted where required) |

### FinalReviewIntegrationTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J066 | `unauthorizedConfigurationIsDeniedBeforeAnyWriteLock` | `ManagementScopeTests.UnauthorizedCommandsDoNotAcquireTheDatabaseWriter` | Adapted | Stage 3 verified (adapted where required) |
| J067 | `anEmptyActiveCatalogIsAUsablePatientPage` | `BookingIntegrationTests.EmptyCatalogAndMalformedPostsRemainUsableAndSafe` | Adapted | Stage 2 verified (adapted where required) |
| J068 | `rejectedBookingPreservesEmployeeServiceAndDateSelection` | `FinalReviewIntegrationTests.RejectedBookingPreservesEmployeeServiceBranchFacilityAndDateSelection` | Adapted | Stage 4 verified; real 409 retains facility/branch/service/employee/date and saves no duplicate |
| J069 | `subtypeEditorCannotChangeAHealthcareEmployeeIntoADoctor` | `ManagementIntegrationTests.DoctorAndHealthcareStaffRemainPolymorphicAndOfferingAssignmentIsIdempotent` | Equivalent | Stage 3 verified (adapted where required) |
| J070 | `renamedSeedFacilityDoesNotTriggerDuplicateInitialization` | `BookingPersistenceTests.RenamedSeedAndDecidedAppointmentSurviveRestartWithoutMovingDates` | Equivalent | Stage 2 verified (adapted where required) |
| J071 | `managerInputIsEscapedRatherThanExecutedInTheView` | `ManagementHttpTests.ForgedEditDepartmentCannotRelocateServiceAndManagerTextIsRazorEncoded` | Adapted | Stage 3 verified (adapted where required) |
| J072 | `overlongAndExpandingLoginNamesFailAsNormalInvalidCredentials` | `FoundationIntegrationTests.WeakPasswordsAndOversizedUsernamesAreRejected + InvalidCredentialsNeverAuthenticate` | Adapted | Stage 1 verified (adapted where required) |
| J073 | `rolePagesProvideKeyboardLandmarksAndLocalAssets(String)[1]` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |
| J074 | `rolePagesProvideKeyboardLandmarksAndLocalAssets(String)[2]` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |
| J075 | `rolePagesProvideKeyboardLandmarksAndLocalAssets(String)[3]` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |

### ManagementIntegrationTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J076 | `shrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations(AppointmentStatus)[1]` | `ManagementScheduleTests.ShrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations` | Equivalent | Stage 3 verified (adapted where required) |
| J077 | `shrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations(AppointmentStatus)[2]` | `ManagementScheduleTests.ShrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations` | Equivalent | Stage 3 verified (adapted where required) |
| J078 | `rejectedReservationsNoLongerPreventAvailabilityRemoval` | `ManagementScheduleTests.RejectedReservationsReleaseAvailabilityAndHistoricalRowsRemain` | Equivalent | Stage 3 verified (adapted where required) |
| J079 | `facilityCreationFirstBranchGrantAndEditsAreAtomic` | `ManagementIntegrationTests.FacilityCreationGrantsOwnershipAndSupportsSingleAndMultipleBranches + InvalidOrDuplicateFacilityCreationLeavesNoPartialBranchOrGrant` | Equivalent | Stage 3 verified (adapted where required) |
| J080 | `deactivationBlocksNewBookingsButPreservesExistingApprovalAndHistory` | `ManagementIntegrationTests.DeactivationBlocksNewBookingsButKeepsHistoryAndAssignedApproval` | Adapted | Stage 3 verified (adapted where required) |
| J081 | `serviceAssignmentIsTheBranchAssignmentAndAvailabilityRequiresIt` | `ManagementScheduleTests.PastAvailabilityAndMissingOrInactiveAssignmentsAreRejected + ShiftsCannotOverlapAcrossBranchesOrDuplicateButMayBeAdjacent` | Equivalent | Stage 3 verified (adapted where required) |
| J082 | `forgedForeignChildIdsAreRejectedEvenUnderAnOwnedFacilityUrl` | `ManagementIntegrationTests.ForgedChildResourcesAreRejectedEvenWhenManagerOwnsTheUrlFacility` | Equivalent | Stage 3 verified (adapted where required) |
| J083 | `managerPagesRenderAndRejectUnauthenticatedWrongRoleAndWrongFacility` | `ManagementHttpTests.ManagerPagesRenderLocalTurkishFormsAndRejectOtherRolesOrFacilities` | Adapted | Stage 3 verified (adapted where required) |
| J084 | `realManagerFormConfigurationConnectsToPatientBookingAndNewStaffApproval` | `ManagementHttpTests.RealManagerFormsLeadToNewDoctorApprovalAndPersistAcrossRestart` | Adapted | Stage 3 verified (adapted where required) |
| J085 | `catalogEditVersusBookingNeverProducesMixedOrStaleTerms` | `ManagementConcurrencyTests.CatalogEditVersusBookingNeverProducesMixedOrStaleTerms` | Adapted | Stage 3 verified (adapted where required) |
| J086 | `serviceEditsKeepBookedSnapshotsAndReservationChecksUseBookedDuration` | `ManagementIntegrationTests.CatalogAndNameEditsKeepBookedSnapshotsAndExactMoney + ManagementScheduleTests.ScheduleProtectionUsesBookedDurationRatherThanCurrentCatalogDuration` | Equivalent | Stage 3 verified (adapted where required) |
| J087 | `employeeShiftsCannotOverlapAcrossBranchesOrDuplicateLocally` | `ManagementScheduleTests.ShiftsCannotOverlapAcrossBranchesOrDuplicateButMayBeAdjacent` | Equivalent | Stage 3 verified (adapted where required) |
| J088 | `availabilityMustFitEntireOpeningUnion(String, String)[1]` | `ManagementScheduleTests.AvailabilityMustFitTheEntireOpeningUnionIncludingLunchGap` | Equivalent | Stage 3 verified (adapted where required) |
| J089 | `availabilityMustFitEntireOpeningUnion(String, String)[2]` | `ManagementScheduleTests.AvailabilityMustFitTheEntireOpeningUnionIncludingLunchGap` | Equivalent | Stage 3 verified (adapted where required) |
| J090 | `availabilityMustFitEntireOpeningUnion(String, String)[3]` | `ManagementScheduleTests.AvailabilityMustFitTheEntireOpeningUnionIncludingLunchGap` | Equivalent | Stage 3 verified (adapted where required) |
| J091 | `unrelatedManagerCannotReadOrModifyAnyConfigurationGroup` | `ManagementIntegrationTests.EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly + ManagementHttpTests.ManagerPagesRenderLocalTurkishFormsAndRejectOtherRolesOrFacilities` | Equivalent | Stage 3 verified (adapted where required) |
| J092 | `unreservedDayCanBeClosedWithoutChangingOtherWeekdays` | `ManagementScheduleTests.ClosingUnreservedDayPreservesOtherWeekdays` | Equivalent | Stage 3 verified (adapted where required) |
| J093 | `openingHourChangesProtectShiftsAndRejectOverlapsButAllowAdjacentPeriods` | `ManagementScheduleTests.HoursProtectFutureUnbookedShiftsAndValidatePeriodsBeforeReplacement` | Equivalent | Stage 3 verified (adapted where required) |
| J094 | `forgedDepartmentOnServiceEditCannotRelocateExistingResource` | `ManagementHttpTests.ForgedEditDepartmentCannotRelocateServiceAndManagerTextIsRazorEncoded` | Equivalent | Stage 3 verified (adapted where required) |
| J095 | `simultaneousCrossBranchShiftCreationHasOneWinner` | `ManagementConcurrencyTests.CompetingOverlappingShiftCreationHasExactlyOneWinner` | Adapted | Stage 3 verified (adapted where required) |
| J096 | `duplicateUsernameCannotStealAnExistingAccount` | `ManagementIntegrationTests.InvalidCredentialsOrExistingAccountsCannotCreateOrTakeOverAProfile + ManagementConcurrencyTests.CompetingStaffRegistrationsCannotDuplicateOrStealALogin` | Equivalent | Stage 3 verified (adapted where required) |
| J097 | `constructorBoundFacilityAndHoursFormsSaveRealConfiguration` | `ManagementHttpTests.RealManagerFormsLeadToNewDoctorApprovalAndPersistAcrossRestart` | Adapted | Stage 3 verified (adapted where required) |
| J098 | `bookingVersusAvailabilityShrinkHasOnlyOneSuccessfulOutcome` | `ManagementConcurrencyTests.BookingVersusAvailabilityShrinkHasExactlyOneSuccessfulOutcome` | Adapted | Stage 3 verified (adapted where required) |
| J099 | `hoursChangeVersusNewAvailabilityCannotLeaveAnInvalidShift` | `ManagementConcurrencyTests.HoursChangeVersusNewAvailabilityCannotLeaveAnInvalidShift` | Adapted | Stage 3 verified (adapted where required) |
| J100 | `staffEditsPreservePolymorphismAndSafeDeactivation` | `ManagementIntegrationTests.DoctorAndHealthcareStaffRemainPolymorphicAndOfferingAssignmentIsIdempotent + DeactivationBlocksNewBookingsButKeepsHistoryAndAssignedApproval` | Equivalent | Stage 3 verified (adapted where required) |
| J101 | `allConfigurationServicesEnforceManagerRoleDirectly(String)[1]` | `ManagementIntegrationTests.EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly` | Equivalent | Stage 3 verified (adapted where required) |
| J102 | `allConfigurationServicesEnforceManagerRoleDirectly(String)[2]` | `ManagementIntegrationTests.EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly` | Equivalent | Stage 3 verified (adapted where required) |
| J103 | `allConfigurationServicesEnforceManagerRoleDirectly(String)[3]` | `ManagementIntegrationTests.EveryManagementServiceEnforcesRoleAndFacilityGrantDirectly` | Equivalent | Stage 3 verified (adapted where required) |

### AuthenticationIntegrationTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J104 | `demoSeedingIsIdempotentAndStoresEncodedPasswords` | `FoundationIntegrationTests.PasswordsAreHashedAndSeederNeverReplacesStoredCredentials` | Adapted | Stage 1 verified (adapted where required) |
| J105 | `portalShowsOnlyAllowedNavigationAndNoPassword` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |
| J106 | `logoutRequiresCsrfAndInvalidatesSession` | `FoundationIntegrationTests.LogoutRequiresAntiforgeryAndClearsTheAuthenticationCookie` | Adapted | Stage 1 verified (adapted where required) |
| J107 | `unauthenticatedRequestsAreRedirectedToLogin` | `FoundationIntegrationTests.UnauthenticatedVisitorsAreRedirectedToLogin` | Adapted | Stage 1 verified (adapted where required) |
| J108 | `h2ConsoleAndUnmappedRoutesAreNotExposed` | `FoundationIntegrationTests.UnimplementedFeaturesAndDatabaseFilesAreNotExposed` | Replaced | Stage 1 verified (adapted where required) |
| J109 | `invalidCredentialsNeverAuthenticate(String, String)[1]` | `FoundationIntegrationTests.InvalidCredentialsNeverAuthenticate` | Adapted | Stage 1 verified (adapted where required) |
| J110 | `invalidCredentialsNeverAuthenticate(String, String)[2]` | `FoundationIntegrationTests.InvalidCredentialsNeverAuthenticate` | Adapted | Stage 1 verified (adapted where required) |
| J111 | `realLoginAllowsOnlyTheAccountsRole(String, String, String, String, String)[1]` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |
| J112 | `realLoginAllowsOnlyTheAccountsRole(String, String, String, String, String)[2]` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |
| J113 | `realLoginAllowsOnlyTheAccountsRole(String, String, String, String, String)[3]` | `FoundationIntegrationTests.RealIdentityLoginGrantsOnlyAssignedRole` | Adapted | Stage 1 verified (adapted where required) |
| J114 | `normalizedUsernameCanAuthenticate` | `FoundationIntegrationTests.LoginAcceptsTrimmedCaseInsensitiveUsernameAndRejectsExternalReturnUrl` | Adapted | Stage 1 verified (adapted where required) |
| J115 | `loginRequiresCsrfToken` | `FoundationIntegrationTests.LoginWithoutAntiforgeryCannotAuthenticate` | Adapted | Stage 1 verified (adapted where required) |
| J116 | `publicPagesAndLocalStylesheetsAreAccessible` | `FoundationIntegrationTests.PublicPagesAndLocalAssetsAreAvailable` | Adapted | Stage 1 verified (adapted where required) |

### NonDemoProfileIntegrationTest

| Reference case | Java execution | C# test / planned target | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J117 | `injectedClockUsesTheConfiguredClinicTimeZone` | `FoundationIntegrationTests.StartupResolvesServicesAndUsesTheMigratedSqliteSchema` | Adapted | Stage 1 verified (adapted where required) |
| J118 | `demoSeedingAndDemoAccessAreAbsentOutsideDemoProfile` | `FoundationIntegrationTests.DemoSeedingIsAbsentUnlessEnvironmentAndSettingBothEnableIt` | Adapted | Stage 1 verified (adapted where required) |

Inventory total: **118 historical Java executions; 197 executed C# tests**. Multiple reference rows may share a C# test, so mapping counts are not additive. All 28 management rows link to executed equivalents/adaptations. Stage 4 resolves the remaining J068 partial status with a real HTTP conflict/selection/persistence regression. The historical stage labels identify when each behavior was implemented; all mapped current C# tests also passed in the final suite. See STAGE4_VERIFICATION.md. No Java tests were rerun.


Stage 2 additions beyond Java/H2 remain: native EF/SQLite enlistment before reads, stale tracker refresh, exact integer storage/constraint checks, HTTP 503 with retained selections, and two Kestrel processes sharing one file. H2 row-lock assertions were replaced by SQLite behavior. Stage 3 adds ten actual configuration races, atomic Identity user/role/profile rollback, reauthorization after writer acquisition, full booked-duration preservation, ongoing appointment protection, manager HTTP form binding, Turkish comma-decimal input without implicit thousands grouping, and new configuration/approval persistence across separate host lifetimes. J003's nullable Java role element has no direct non-nullable C# enum equivalent; invalid enum/empty role snapshots are covered without introducing nullable domain roles. J026's local same-day rule belongs to the application timezone boundary rather than a generic UTC interval object.

Stage 3 preserves all 113 prior discovered cases. The public-homepage test now expects implemented management copy instead of the word “later”; two unmapped-route theory inputs changed from the newly implemented manager URLs to deferred payment/medical-record routes. No test was removed or skipped. Historical Stage 1/2 reports describe the behavior at those earlier checkpoints.

Stage 4 preserves all 191 cases and adds FinalReviewIntegrationTests: three long-fraction fee inputs that reproduced rounding before the fix, complete conflict-page selection recovery, specific Turkish validation without password echo/partial account creation, and role revocation with an already issued cookie/direct service call. Final restore/build/test/model consistency and Release/browser/restart results are in STAGE4_VERIFICATION.md. No UI snapshot or implementation-mirroring tests were added merely to increase the count.
