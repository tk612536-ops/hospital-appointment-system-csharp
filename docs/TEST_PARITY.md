# Java → C# test parity and verification plan

Status: Stage 0 planning only. **No C# test project, test implementation or execution exists.** Proposed test names below are planning identifiers, not links to existing C# code.

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
| **Total** | **118** | **All C# cases are not implemented / not run** |

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

The following rows inventory each execution in the existing Java XML reports, including indexed parameter cases. Preserve source row values from the Java test source when implementing; index numbers are report locators, not independent requirement IDs. All target tests remain proposals. Class prefixes organize intended coverage; individual cases may move between the approved test projects when their boundary requires it.

### UserAccountTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J001 | `rejectsAnUnencodedPassword` | `IdentityAccountTests.IdentityCreatesAndVerifiesHashedPasswords` | Replaced | Not implemented / not run |
| J002 | `rolesAreComposedAndDefensivelyCopied` | `IdentityAccountTests.RolesAreComposedAndDefensivelyCopied` | Adapted | Not implemented / not run |
| J003 | `rejectsNullRoleElements` | `IdentityAccountTests.RejectsNullRoleElements` | Adapted | Not implemented / not run |
| J004 | `rejectsAccountsWithoutRoles` | `IdentityAccountTests.RejectsAccountsWithoutRoles` | Adapted | Not implemented / not run |
| J005 | `rejectsBlankUsername(String)[1]` | `IdentityAccountTests.RejectsBlankUsername_Case1` | Adapted | Not implemented / not run |
| J006 | `rejectsBlankUsername(String)[2]` | `IdentityAccountTests.RejectsBlankUsername_Case2` | Adapted | Not implemented / not run |
| J007 | `rejectsBlankUsername(String)[3]` | `IdentityAccountTests.RejectsBlankUsername_Case3` | Adapted | Not implemented / not run |
| J008 | `rejectsBlankUsername(String)[4]` | `IdentityAccountTests.RejectsBlankUsername_Case4` | Adapted | Not implemented / not run |
| J009 | `normalizationCannotExpandAUsernamePastItsDatabaseLimit` | `IdentityAccountTests.IdentityNormalizationAndUsernameBoundsAreSafe` | Replaced | Not implemented / not run |
| J010 | `rejectsAnOverlongUsernameBeforePersistence` | `IdentityAccountTests.RejectsAnOverlongUsernameBeforePersistence` | Adapted | Not implemented / not run |
| J011 | `normalizesLoginIdentityWithoutChangingDisplayName` | `IdentityAccountTests.NormalizesLoginIdentityWithoutChangingDisplayName` | Adapted | Not implemented / not run |

### AppointmentDomainTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J012 | `pendingApprovalAndRejectionProtectTheirTransitionsAndAudit` | `AppointmentDomainTests.PendingApprovalAndRejectionProtectTheirTransitionsAndAudit` | Equivalent | Not implemented / not run |
| J013 | `onlyManagersCanReceiveFacilityGrants` | `AppointmentDomainTests.OnlyManagerRoleCanCreateOrExerciseFacilityGrants` | Adapted | Not implemented / not run |
| J014 | `invalidDecisionInputCannotPartiallyChangeAuditState` | `AppointmentDomainTests.InvalidDecisionInputCannotPartiallyChangeAuditState` | Equivalent | Not implemented / not run |
| J015 | `offeringCannotJoinDifferentEmployingFacilities` | `AppointmentDomainTests.OfferingCannotJoinDifferentEmployingFacilities` | Equivalent | Not implemented / not run |
| J016 | `differentStaffTypesHonorTheSameContractWithoutTypeSwitches` | `AppointmentDomainTests.DifferentStaffTypesHonorTheSameContractWithoutTypeSwitches` | Equivalent | Not implemented / not run |
| J017 | `appointmentMustMatchServerSideDuration` | `AppointmentDomainTests.AppointmentMustMatchServerSideDuration` | Equivalent | Not implemented / not run |
| J018 | `catalogChangesCannotRewriteBookedSnapshots` | `AppointmentDomainTests.CatalogChangesCannotRewriteBookedSnapshots` | Equivalent | Not implemented / not run |
| J019 | `rejectsInvalidServiceDurations(int)[1]` | `AppointmentDomainTests.RejectsInvalidServiceDurations_Case1` | Equivalent | Not implemented / not run |
| J020 | `rejectsInvalidServiceDurations(int)[2]` | `AppointmentDomainTests.RejectsInvalidServiceDurations_Case2` | Equivalent | Not implemented / not run |
| J021 | `rejectsInvalidServiceDurations(int)[3]` | `AppointmentDomainTests.RejectsInvalidServiceDurations_Case3` | Equivalent | Not implemented / not run |
| J022 | `rejectsNegativeOrFractionalCentFees` | `AppointmentDomainTests.RejectsNegativeOrFractionalCentFees` | Equivalent | Not implemented / not run |

### ConfigurationDomainTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J023 | `professionalEditsRemainPolymorphicAndDoNotChangeEmployeeType` | `ConfigurationDomainTests.ProfessionalEditsRemainPolymorphicAndDoNotChangeEmployeeType` | Equivalent | Not implemented / not run |
| J024 | `invalidFacilityTypeCannotPartiallyRenameIt` | `ConfigurationDomainTests.InvalidFacilityTypeCannotPartiallyRenameIt` | Equivalent | Not implemented / not run |
| J025 | `renamingProtectsOwnershipAndValidatesState` | `ConfigurationDomainTests.RenamingProtectsOwnershipAndValidatesState` | Equivalent | Not implemented / not run |
| J026 | `reschedulingPreservesResourceLinksAndRejectsOvernightIntervals` | `ConfigurationDomainTests.ReschedulingPreservesResourceLinksAndRejectsOvernightIntervals` | Equivalent | Not implemented / not run |
| J027 | `activeStatesChangeEligibilityWithoutRemovingAssociations` | `ConfigurationDomainTests.ActiveStatesChangeEligibilityWithoutRemovingAssociations` | Equivalent | Not implemented / not run |

### TimeIntervalTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J028 | `rejectsZeroAndNegativeLength` | `TimeIntervalTests.RejectsZeroAndNegativeLength` | Equivalent | Not implemented / not run |
| J029 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[1]` | `TimeIntervalTests.OverlapIsSymmetricAndEndExclusive_Case1` | Equivalent | Not implemented / not run |
| J030 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[2]` | `TimeIntervalTests.OverlapIsSymmetricAndEndExclusive_Case2` | Equivalent | Not implemented / not run |
| J031 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[3]` | `TimeIntervalTests.OverlapIsSymmetricAndEndExclusive_Case3` | Equivalent | Not implemented / not run |
| J032 | `overlapIsSymmetricAndEndExclusive(String, String, String, String, boolean)[4]` | `TimeIntervalTests.OverlapIsSymmetricAndEndExclusive_Case4` | Equivalent | Not implemented / not run |
| J033 | `continuousCoverageMergesAdjacentButNeverBridgesGaps` | `TimeIntervalTests.ContinuousCoverageMergesAdjacentButNeverBridgesGaps` | Equivalent | Not implemented / not run |
| J034 | `containmentRequiresTheWholeIntervalIncludingTheEndBoundary` | `TimeIntervalTests.ContainmentRequiresTheWholeIntervalIncludingTheEndBoundary` | Equivalent | Not implemented / not run |

### UserAccountRepositoryTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J035 | `databaseRejectsDuplicateNormalizedUsernames` | `IdentityPersistenceTests.DatabaseRejectsDuplicateNormalizedUsernames` | Adapted | Not implemented / not run |
| J036 | `persistsAndReloadsAccountWithMultipleRoles` | `IdentityPersistenceTests.PersistsAndReloadsAccountWithMultipleRoles` | Adapted | Not implemented / not run |

### BookingIntegrationTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J037 | `simultaneousBackToBackRequestsBothCommit` | `BookingIntegrationTests.SimultaneousBackToBackRequestsBothCommit` | Adapted | Not implemented / not run |
| J038 | `branchLunchGapAndClosingBoundaryRejectPartialCoverage(String)[1]` | `BookingIntegrationTests.BranchLunchGapAndClosingBoundaryRejectPartialCoverage_Case1` | Equivalent | Not implemented / not run |
| J039 | `branchLunchGapAndClosingBoundaryRejectPartialCoverage(String)[2]` | `BookingIntegrationTests.BranchLunchGapAndClosingBoundaryRejectPartialCoverage_Case2` | Equivalent | Not implemented / not run |
| J040 | `branchLunchGapAndClosingBoundaryRejectPartialCoverage(String)[3]` | `BookingIntegrationTests.BranchLunchGapAndClosingBoundaryRejectPartialCoverage_Case3` | Equivalent | Not implemented / not run |
| J041 | `staleCatalogVersionRejectedAndExistingSnapshotsRemainStable` | `BookingIntegrationTests.StaleCatalogVersionRejectedAndExistingSnapshotsRemainStable` | Equivalent | Not implemented / not run |
| J042 | `candidatesUseTheSameFullDurationAndPatientConflictRules` | `BookingIntegrationTests.CandidatesUseTheSameFullDurationAndPatientConflictRules` | Equivalent | Not implemented / not run |
| J043 | `databaseLockTimeoutRollsBackWithoutPartialReservation` | `BookingIntegrationTests.SqliteBusyTimeoutRollsBackWithoutPartialReservation` | Replaced | Not implemented / not run |
| J044 | `adjacentAvailabilityWindowsJoinButPositiveGapsDoNot` | `BookingIntegrationTests.AdjacentAvailabilityWindowsJoinButPositiveGapsDoNot` | Equivalent | Not implemented / not run |
| J045 | `patientsStaffAndManagersAreRestrictedByObjectOwnership` | `BookingIntegrationTests.PatientsStaffAndManagersAreRestrictedByObjectOwnership` | Equivalent | Not implemented / not run |
| J046 | `sameEmployeeConflictQueryNeverFiltersByBranch` | `BookingIntegrationTests.SameEmployeeConflictQueryNeverFiltersByBranch` | Equivalent | Not implemented / not run |
| J047 | `simultaneousSamePatientDifferentStaffAndBranchesHaveOneWinner` | `BookingIntegrationTests.SimultaneousSamePatientDifferentStaffAndBranchesHaveOneWinner` | Adapted | Not implemented / not run |
| J048 | `wholeDurationEndingAtAvailabilityBoundaryIsAcceptedAndPersisted` | `BookingIntegrationTests.WholeDurationEndingAtAvailabilityBoundaryIsAcceptedAndPersisted` | Equivalent | Not implemented / not run |
| J049 | `samePatientCannotOverlapDifferentStaffOrBranches` | `BookingIntegrationTests.SamePatientCannotOverlapDifferentStaffOrBranches` | Equivalent | Not implemented / not run |
| J050 | `pendingAndApprovedBlockButRejectedReleasesTime` | `BookingIntegrationTests.PendingAndApprovedBlockButRejectedReleasesTime` | Adapted | Not implemented / not run |
| J051 | `backToBackAppointmentsCanBothReserve` | `BookingIntegrationTests.BackToBackAppointmentsCanBothReserve` | Equivalent | Not implemented / not run |
| J052 | `malformedOrConflictingBookingProducesFeedbackWithoutSaving` | `BookingIntegrationTests.MalformedOrConflictingBookingProducesFeedbackWithoutSaving` | Adapted | Not implemented / not run |
| J053 | `simultaneousSameStaffRequestsWithNoExistingAppointmentsHaveOneWinner` | `BookingIntegrationTests.SimultaneousSameStaffRequestsWithNoExistingAppointmentsHaveOneWinner` | Adapted | Not implemented / not run |
| J054 | `pastOffGridAndSecondPrecisionStartsAreRejected` | `BookingIntegrationTests.PastOffGridAndSecondPrecisionStartsAreRejected` | Equivalent | Not implemented / not run |
| J055 | `healthcareStaffBooksThroughTheSharedFlowAndJoinedMapping` | `BookingIntegrationTests.HealthcareStaffBooksThroughSharedFlowAndTphMapping` | Adapted | Not implemented / not run |
| J056 | `freeStartIsInsufficientWithoutWholeDurationCoverage(String, String)[1]` | `BookingIntegrationTests.FreeStartIsInsufficientWithoutWholeDurationCoverage_Case1` | Equivalent | Not implemented / not run |
| J057 | `freeStartIsInsufficientWithoutWholeDurationCoverage(String, String)[2]` | `BookingIntegrationTests.FreeStartIsInsufficientWithoutWholeDurationCoverage_Case2` | Equivalent | Not implemented / not run |
| J058 | `freeStartIsInsufficientWithoutWholeDurationCoverage(String, String)[3]` | `BookingIntegrationTests.FreeStartIsInsufficientWithoutWholeDurationCoverage_Case3` | Equivalent | Not implemented / not run |
| J059 | `simultaneousDecisionsCanTransitionPendingOnlyOnce` | `BookingIntegrationTests.SimultaneousDecisionsCanTransitionPendingOnlyOnce` | Adapted | Not implemented / not run |
| J060 | `joinedStaffMappingActuallyLocksTheSharedStaffTable` | `BookingIntegrationTests.ImmediateWriterTransactionPrecedesMutableReads` | Replaced | Not implemented / not run |
| J061 | `renderedBookingListDetailsAndDecisionFormsWorkWithoutTrustedClientTerms` | `BookingIntegrationTests.RenderedBookingListDetailsAndDecisionFormsWorkWithoutTrustedClientTerms` | Adapted | Not implemented / not run |
| J062 | `workerListsRespectAssignedEmployeeAndFacilityGrants` | `BookingIntegrationTests.WorkerListsRespectAssignedEmployeeAndFacilityGrants` | Equivalent | Not implemented / not run |
| J063 | `simultaneousCrossBranchStaffRequestsHaveOneWinner` | `BookingIntegrationTests.SimultaneousCrossBranchStaffRequestsHaveOneWinner` | Adapted | Not implemented / not run |

### DemoDataIntegrationTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J064 | `demoAppointmentsUseFutureDatesAndSeedingNeverMovesOrDuplicatesThem` | `DemoDataIntegrationTests.DemoAppointmentsUseFutureDatesAndSeedingNeverMovesOrDuplicatesThem` | Equivalent | Not implemented / not run |
| J065 | `bothPersistedSubtypesWorkThroughTheAbstractRepositoryContract` | `DemoDataIntegrationTests.BothPersistedSubtypesWorkThroughTheAbstractRepositoryContract` | Equivalent | Not implemented / not run |

### FinalReviewIntegrationTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J066 | `unauthorizedConfigurationIsDeniedBeforeAnyWriteLock` | `FinalReviewIntegrationTests.UnauthorizedConfigurationDeniedBeforeWriterAcquisition` | Adapted | Not implemented / not run |
| J067 | `anEmptyActiveCatalogIsAUsablePatientPage` | `FinalReviewIntegrationTests.AnEmptyActiveCatalogIsAUsablePatientPage` | Adapted | Not implemented / not run |
| J068 | `rejectedBookingPreservesEmployeeServiceAndDateSelection` | `FinalReviewIntegrationTests.RejectedBookingPreservesEmployeeServiceAndDateSelection` | Adapted | Not implemented / not run |
| J069 | `subtypeEditorCannotChangeAHealthcareEmployeeIntoADoctor` | `FinalReviewIntegrationTests.SubtypeEditorCannotChangeAHealthcareEmployeeIntoADoctor` | Equivalent | Not implemented / not run |
| J070 | `renamedSeedFacilityDoesNotTriggerDuplicateInitialization` | `FinalReviewIntegrationTests.RenamedSeedFacilityDoesNotTriggerDuplicateInitialization` | Equivalent | Not implemented / not run |
| J071 | `managerInputIsEscapedRatherThanExecutedInTheView` | `FinalReviewIntegrationTests.ManagerInputIsEscapedRatherThanExecutedInTheView` | Adapted | Not implemented / not run |
| J072 | `overlongAndExpandingLoginNamesFailAsNormalInvalidCredentials` | `FinalReviewIntegrationTests.MalformedOrOverlongIdentityLoginFailsNormally` | Adapted | Not implemented / not run |
| J073 | `rolePagesProvideKeyboardLandmarksAndLocalAssets(String)[1]` | `FinalReviewIntegrationTests.RolePagesProvideKeyboardLandmarksAndLocalAssets_Case1` | Adapted | Not implemented / not run |
| J074 | `rolePagesProvideKeyboardLandmarksAndLocalAssets(String)[2]` | `FinalReviewIntegrationTests.RolePagesProvideKeyboardLandmarksAndLocalAssets_Case2` | Adapted | Not implemented / not run |
| J075 | `rolePagesProvideKeyboardLandmarksAndLocalAssets(String)[3]` | `FinalReviewIntegrationTests.RolePagesProvideKeyboardLandmarksAndLocalAssets_Case3` | Adapted | Not implemented / not run |

### ManagementIntegrationTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J076 | `shrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations(AppointmentStatus)[1]` | `ManagementIntegrationTests.ShrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations_Case1` | Equivalent | Not implemented / not run |
| J077 | `shrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations(AppointmentStatus)[2]` | `ManagementIntegrationTests.ShrinkingRemovingOrMovingAvailabilityCannotInvalidateReservations_Case2` | Equivalent | Not implemented / not run |
| J078 | `rejectedReservationsNoLongerPreventAvailabilityRemoval` | `ManagementIntegrationTests.RejectedReservationsNoLongerPreventAvailabilityRemoval` | Equivalent | Not implemented / not run |
| J079 | `facilityCreationFirstBranchGrantAndEditsAreAtomic` | `ManagementIntegrationTests.FacilityCreationFirstBranchGrantAndEditsAreAtomic` | Equivalent | Not implemented / not run |
| J080 | `deactivationBlocksNewBookingsButPreservesExistingApprovalAndHistory` | `ManagementIntegrationTests.DeactivationBlocksNewBookingsButPreservesExistingApprovalAndHistory` | Adapted | Not implemented / not run |
| J081 | `serviceAssignmentIsTheBranchAssignmentAndAvailabilityRequiresIt` | `ManagementIntegrationTests.ServiceAssignmentIsTheBranchAssignmentAndAvailabilityRequiresIt` | Equivalent | Not implemented / not run |
| J082 | `forgedForeignChildIdsAreRejectedEvenUnderAnOwnedFacilityUrl` | `ManagementIntegrationTests.ForgedForeignChildIdsAreRejectedEvenUnderAnOwnedFacilityUrl` | Equivalent | Not implemented / not run |
| J083 | `managerPagesRenderAndRejectUnauthenticatedWrongRoleAndWrongFacility` | `ManagementIntegrationTests.ManagerPagesRenderAndRejectUnauthenticatedWrongRoleAndWrongFacility` | Adapted | Not implemented / not run |
| J084 | `realManagerFormConfigurationConnectsToPatientBookingAndNewStaffApproval` | `ManagementIntegrationTests.RealManagerFormConfigurationConnectsToPatientBookingAndNewStaffApproval` | Adapted | Not implemented / not run |
| J085 | `catalogEditVersusBookingNeverProducesMixedOrStaleTerms` | `ManagementIntegrationTests.CatalogEditVersusBookingNeverProducesMixedOrStaleTerms` | Adapted | Not implemented / not run |
| J086 | `serviceEditsKeepBookedSnapshotsAndReservationChecksUseBookedDuration` | `ManagementIntegrationTests.ServiceEditsKeepBookedSnapshotsAndReservationChecksUseBookedDuration` | Equivalent | Not implemented / not run |
| J087 | `employeeShiftsCannotOverlapAcrossBranchesOrDuplicateLocally` | `ManagementIntegrationTests.EmployeeShiftsCannotOverlapAcrossBranchesOrDuplicateLocally` | Equivalent | Not implemented / not run |
| J088 | `availabilityMustFitEntireOpeningUnion(String, String)[1]` | `ManagementIntegrationTests.AvailabilityMustFitEntireOpeningUnion_Case1` | Equivalent | Not implemented / not run |
| J089 | `availabilityMustFitEntireOpeningUnion(String, String)[2]` | `ManagementIntegrationTests.AvailabilityMustFitEntireOpeningUnion_Case2` | Equivalent | Not implemented / not run |
| J090 | `availabilityMustFitEntireOpeningUnion(String, String)[3]` | `ManagementIntegrationTests.AvailabilityMustFitEntireOpeningUnion_Case3` | Equivalent | Not implemented / not run |
| J091 | `unrelatedManagerCannotReadOrModifyAnyConfigurationGroup` | `ManagementIntegrationTests.UnrelatedManagerCannotReadOrModifyAnyConfigurationGroup` | Equivalent | Not implemented / not run |
| J092 | `unreservedDayCanBeClosedWithoutChangingOtherWeekdays` | `ManagementIntegrationTests.UnreservedDayCanBeClosedWithoutChangingOtherWeekdays` | Equivalent | Not implemented / not run |
| J093 | `openingHourChangesProtectShiftsAndRejectOverlapsButAllowAdjacentPeriods` | `ManagementIntegrationTests.OpeningHourChangesProtectShiftsAndRejectOverlapsButAllowAdjacentPeriods` | Equivalent | Not implemented / not run |
| J094 | `forgedDepartmentOnServiceEditCannotRelocateExistingResource` | `ManagementIntegrationTests.ForgedDepartmentOnServiceEditCannotRelocateExistingResource` | Equivalent | Not implemented / not run |
| J095 | `simultaneousCrossBranchShiftCreationHasOneWinner` | `ManagementIntegrationTests.SimultaneousCrossBranchShiftCreationHasOneWinner` | Adapted | Not implemented / not run |
| J096 | `duplicateUsernameCannotStealAnExistingAccount` | `ManagementIntegrationTests.DuplicateUsernameCannotStealAnExistingAccount` | Equivalent | Not implemented / not run |
| J097 | `constructorBoundFacilityAndHoursFormsSaveRealConfiguration` | `ManagementIntegrationTests.ConstructorBoundFacilityAndHoursFormsSaveRealConfiguration` | Adapted | Not implemented / not run |
| J098 | `bookingVersusAvailabilityShrinkHasOnlyOneSuccessfulOutcome` | `ManagementIntegrationTests.BookingVersusAvailabilityShrinkHasOnlyOneSuccessfulOutcome` | Adapted | Not implemented / not run |
| J099 | `hoursChangeVersusNewAvailabilityCannotLeaveAnInvalidShift` | `ManagementIntegrationTests.HoursChangeVersusNewAvailabilityCannotLeaveAnInvalidShift` | Adapted | Not implemented / not run |
| J100 | `staffEditsPreservePolymorphismAndSafeDeactivation` | `ManagementIntegrationTests.StaffEditsPreservePolymorphismAndSafeDeactivation` | Equivalent | Not implemented / not run |
| J101 | `allConfigurationServicesEnforceManagerRoleDirectly(String)[1]` | `ManagementIntegrationTests.AllConfigurationServicesEnforceManagerRoleDirectly_Case1` | Equivalent | Not implemented / not run |
| J102 | `allConfigurationServicesEnforceManagerRoleDirectly(String)[2]` | `ManagementIntegrationTests.AllConfigurationServicesEnforceManagerRoleDirectly_Case2` | Equivalent | Not implemented / not run |
| J103 | `allConfigurationServicesEnforceManagerRoleDirectly(String)[3]` | `ManagementIntegrationTests.AllConfigurationServicesEnforceManagerRoleDirectly_Case3` | Equivalent | Not implemented / not run |

### AuthenticationIntegrationTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J104 | `demoSeedingIsIdempotentAndStoresEncodedPasswords` | `AuthenticationIntegrationTests.DemoSeedingIsIdempotentAndStoresEncodedPasswords` | Adapted | Not implemented / not run |
| J105 | `portalShowsOnlyAllowedNavigationAndNoPassword` | `AuthenticationIntegrationTests.PortalShowsOnlyAllowedNavigationAndNoPassword` | Adapted | Not implemented / not run |
| J106 | `logoutRequiresCsrfAndInvalidatesSession` | `AuthenticationIntegrationTests.LogoutRequiresAntiforgeryAndClearsAuthentication` | Adapted | Not implemented / not run |
| J107 | `unauthenticatedRequestsAreRedirectedToLogin` | `AuthenticationIntegrationTests.UnauthenticatedRequestsAreRedirectedToLogin` | Adapted | Not implemented / not run |
| J108 | `h2ConsoleAndUnmappedRoutesAreNotExposed` | `AuthenticationIntegrationTests.DatabaseDebugAndUnknownEndpointsAreUnavailable` | Replaced | Not implemented / not run |
| J109 | `invalidCredentialsNeverAuthenticate(String, String)[1]` | `AuthenticationIntegrationTests.InvalidCredentialsNeverAuthenticate_Case1` | Adapted | Not implemented / not run |
| J110 | `invalidCredentialsNeverAuthenticate(String, String)[2]` | `AuthenticationIntegrationTests.InvalidCredentialsNeverAuthenticate_Case2` | Adapted | Not implemented / not run |
| J111 | `realLoginAllowsOnlyTheAccountsRole(String, String, String, String, String)[1]` | `AuthenticationIntegrationTests.RealLoginAllowsOnlyTheAccountsRole_Case1` | Adapted | Not implemented / not run |
| J112 | `realLoginAllowsOnlyTheAccountsRole(String, String, String, String, String)[2]` | `AuthenticationIntegrationTests.RealLoginAllowsOnlyTheAccountsRole_Case2` | Adapted | Not implemented / not run |
| J113 | `realLoginAllowsOnlyTheAccountsRole(String, String, String, String, String)[3]` | `AuthenticationIntegrationTests.RealLoginAllowsOnlyTheAccountsRole_Case3` | Adapted | Not implemented / not run |
| J114 | `normalizedUsernameCanAuthenticate` | `AuthenticationIntegrationTests.NormalizedUsernameCanAuthenticate` | Adapted | Not implemented / not run |
| J115 | `loginRequiresCsrfToken` | `AuthenticationIntegrationTests.LoginRequiresCsrfToken` | Adapted | Not implemented / not run |
| J116 | `publicPagesAndLocalStylesheetsAreAccessible` | `AuthenticationIntegrationTests.PublicPagesAndLocalStylesheetsAreAccessible` | Adapted | Not implemented / not run |

### NonDemoProfileIntegrationTest

| Reference case | Java execution | Proposed C# test | Treatment | C# status |
| --- | --- | --- | --- | --- |
| J117 | `injectedClockUsesTheConfiguredClinicTimeZone` | `NonDemoIntegrationTests.InjectedClockUsesTheConfiguredClinicTimeZone` | Adapted | Not implemented / not run |
| J118 | `demoSeedingAndDemoAccessAreAbsentOutsideDemoProfile` | `NonDemoIntegrationTests.DemoSeedingAndDemoAccessAreAbsentOutsideDemoProfile` | Adapted | Not implemented / not run |

Inventory total: **118 historical Java executions; 0 implemented or executed C# tests**. Update mappings and verification evidence during authorized implementation stages.
