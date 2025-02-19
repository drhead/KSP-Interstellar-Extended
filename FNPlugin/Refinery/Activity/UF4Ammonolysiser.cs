﻿using System.Linq;
using FNPlugin.Constants;
using FNPlugin.Extensions;
using FNPlugin.Resources;
using KSP.Localization;
using UnityEngine;

namespace FNPlugin.Refinery.Activity
{
    class UF4Ammonolysiser : RefineryActivity
    {
        public UF4Ammonolysiser()
        {
            ActivityName = "Uranium Tetrafluoride Ammonolysis";
            PowerRequirements = PluginSettings.Config.BaseUraniumAmmonolysisPowerConsumption;
            EnergyPerTon = 1 / GameConstants.baseUraniumAmmonolysisRate;
        }

        double _ammoniaDensity;
        double _uraniumTetrafluorideDensity;
        double _uraniumNitrideDensity;

        double _ammoniaConsumptionRate;
        double _uraniumTetrafluorideConsumptionRate;
        double _uraniumNitrideProductionRate;

        public override string Status => string.Copy(_status);
        public override RefineryType RefineryType => RefineryType.Synthesize;

        public override bool HasActivityRequirements()
        {
            return _part.GetConnectedResources(ResourceSettings.Config.UraniumTetraflouride)
                .Any(rs => rs.amount > 0) && _part.GetConnectedResources(ResourceSettings.Config.AmmoniaLqd).Any(rs => rs.amount > 0);
        }

        public override void Initialize(Part localPart, InterstellarRefineryController controller)
        {
            base.Initialize(localPart, controller);

            _ammoniaDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.AmmoniaLqd).density;
            _uraniumTetrafluorideDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.UraniumTetraflouride).density;
            _uraniumNitrideDensity = PartResourceLibrary.Instance.GetDefinition(ResourceSettings.Config.UraniumNitride).density;
        }

        public override void UpdateFrame(double rateMultiplier, double powerFraction, double productionModifier, bool allowOverflow, double fixedDeltaTime, bool isStartup = false)
        {
            _current_power = PowerRequirements * rateMultiplier;
            _current_rate = CurrentPower / EnergyPerTon;
            double uf4Fraction = _current_rate * 1.24597 / _uraniumTetrafluorideDensity;
            double ammoniaFraction = _current_rate * 0.901 / _ammoniaDensity;
            _uraniumTetrafluorideConsumptionRate = _part.RequestResource(ResourceSettings.Config.UraniumTetraflouride, uf4Fraction * fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) * _uraniumTetrafluorideDensity / fixedDeltaTime;
            _ammoniaConsumptionRate = _part.RequestResource(ResourceSettings.Config.AmmoniaLqd, ammoniaFraction * fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) * _ammoniaDensity / fixedDeltaTime;

            if (_ammoniaConsumptionRate > 0 && _uraniumTetrafluorideConsumptionRate > 0)
                _uraniumNitrideProductionRate = -_part.RequestResource(ResourceSettings.Config.UraniumNitride, -_uraniumTetrafluorideConsumptionRate / 1.24597 / _uraniumNitrideDensity * fixedDeltaTime, ResourceFlowMode.ALL_VESSEL) / fixedDeltaTime * _uraniumNitrideDensity;

            UpdateStatusMessage();
        }

        public override void UpdateGUI()
        {
            base.UpdateGUI();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Power"), _bold_label, GUILayout.Width(labelWidth));//"Power"
            GUILayout.Label(PluginHelper.GetFormattedPowerString(CurrentPower) + "/" + PluginHelper.GetFormattedPowerString(PowerRequirements), _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_AmmonaConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Ammona Consumption Rate"
            GUILayout.Label(_ammoniaConsumptionRate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_ConsumptionRate"), _bold_label, GUILayout.Width(labelWidth));//"Uranium Tetraflouride Consumption Rate"
            GUILayout.Label(_uraniumTetrafluorideConsumptionRate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_ProductionRate"), _bold_label, GUILayout.Width(labelWidth));//"Uranium Nitride Production Rate"
            GUILayout.Label(_uraniumNitrideProductionRate * GameConstants.SECONDS_IN_HOUR + " mT/hour", _value_label, GUILayout.Width(valueWidth));
            GUILayout.EndHorizontal();
        }

        private void UpdateStatusMessage()
        {
            if (_uraniumNitrideProductionRate > 0)
            {
                _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg1");//"Uranium Tetraflouride Ammonolysis Process Ongoing"
            } else if (CurrentPower <= 0.01*PowerRequirements)
            {
                _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg2");//"Insufficient Power"
            }
            else
            {
                if (_ammoniaConsumptionRate > 0 && _uraniumTetrafluorideConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg3");//"Insufficient Storage"
                else if (_ammoniaConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg4");//"Uranium Tetraflouride Deprived"
                else if (_uraniumTetrafluorideConsumptionRate > 0)
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg5");//"Ammonia Deprived"
                else
                    _status = Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Statumsg6");//"UF4 and Ammonia Deprived"

            }
        }
        public override void PrintMissingResources()
        {
            if (!_part.GetConnectedResources(ResourceSettings.Config.AmmoniaLqd).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Postmsg") + " " + ResourceSettings.Config.AmmoniaLqd, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
            if (!_part.GetConnectedResources(ResourceSettings.Config.UraniumTetraflouride).Any(rs => rs.amount > 0))
                ScreenMessages.PostScreenMessage(Localizer.Format("#LOC_KSPIE_UF4Ammonolysiser_Postmsg") + " " + ResourceSettings.Config.UraniumTetraflouride, 3.0f, ScreenMessageStyle.UPPER_CENTER);//Missing
        }
    }
}
