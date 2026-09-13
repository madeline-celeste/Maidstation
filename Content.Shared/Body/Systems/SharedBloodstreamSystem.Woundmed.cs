using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared._Shitmed.Medical.Surgery.Consciousness;
using Content.Shared._Shitmed.Medical.Surgery.Traumas.Components;
using Content.Shared._Shitmed.Medical.Surgery.Wounds.Components;
using Content.Shared.Body.Components;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBloodstreamSystem : EntitySystem
{
    private float _bloodlossMultiplier = 4f; // Goobstation

    private void InitializeWoundmed()
    {
        InitializeWounds();

        Subs.CVar(_cfg, GoobCVars.BleedMultiplier, value => _bloodlossMultiplier = value, true);
    }

    private void UpdateWoundmed()
    {
        var query = EntityQueryEnumerator<BloodstreamComponent>();
        while (query.MoveNext(out var uid, out var bloodstream))
        {
            var total = FixedPoint2.Zero;
            foreach (var (bodyPart, _) in _body.GetBodyChildren(uid))
            {
                var totalPartBleeds = FixedPoint2.Zero;
                foreach (var (wound, _) in _wound.GetWoundableWounds(bodyPart))
                {
                    if (!TryComp<BleedInflicterComponent>(wound, out var bleeds))
                        continue;

                    total += bleeds.BleedingAmount;
                    totalPartBleeds += bleeds.BleedingAmount;
                }

                if (TryComp<WoundableComponent>(bodyPart, out var woundable)
                    && woundable.Bleeds != totalPartBleeds)
                {
                    woundable.Bleeds = totalPartBleeds;
                    Dirty(bodyPart, woundable);
                }
            }

            if (!SolutionContainer.ResolveSolution(uid, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
                continue;

            var missingBlood = bloodstream.BloodReferenceSolution.Volume - bloodstream.BloodSolution.Value.Comp.Solution.Volume;

            bloodstream.BleedAmountFromWounds = (float) total;

            if (_consciousness.TryGetNerveSystem(uid, out var nerveSys))
            {
                if (!_consciousness.SetConsciousnessModifier(
                        uid,
                        nerveSys.Value,
                        -missingBlood / 4,
                        identifier: "Bleeding",
                        type: ConsciousnessModType.Pain))
                {
                    _consciousness.AddConsciousnessModifier(
                        uid,
                        nerveSys.Value,
                        -missingBlood / 4,
                        identifier: "Bleeding",
                        type: ConsciousnessModType.Pain);
                }
            }

            bloodstream.BleedAmount = bloodstream.BleedAmountFromWounds + bloodstream.BleedAmountNotFromWounds;
            bloodstream.BleedAmount = Math.Clamp(bloodstream.BleedAmount, 0, bloodstream.MaxBleedAmount);

            DirtyFields(uid, bloodstream, null, nameof(BloodstreamComponent.BleedAmount), nameof(BloodstreamComponent.BleedAmountFromWounds));

            if (bloodstream.BleedAmount == 0)
                _alertsSystem.ClearAlert(uid, bloodstream.BleedingAlert);
            else
            {
                var severity = (short) Math.Clamp(Math.Round(bloodstream.BleedAmount, MidpointRounding.ToZero), 0, 10);
                _alertsSystem.ShowAlert(uid, bloodstream.BleedingAlert, severity);
            }
        }
    }
}