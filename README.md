# Willy — TIA Portal Tank Systeem

TIA Portal V20 project: tank alarm monitoring (PLC + HMI) en sounding-to-volume
interpolatie voor 6 scheepstanks, met een Openness API (C#) automatiseringsscript
om het TIA-project op te bouwen.

## Structuur

```
PLC/SCL/            SCL-broncode voor UDT's, function blocks en de globale DB.
                     Deze bestanden zijn de invoer voor "Generate blocks from
                     source" in TIA Portal (handmatig of via het Openness-script).

Openness/            C#-consoletoepassing die het TIA-project via de Openness
                     API opbouwt: PLC-software aanmaken/openen, SCL-bronnen
                     importeren, DB_Tanks vullen vanuit config, tag-tabellen
                     aanmaken en (best-effort) HMI-schermen scaffolden.
```

## PLC-architectuur (samenvatting)

- `UDT_Alarm` — status/config van één alarm (setpoint, hysterese, delay, inhibit,
  accept, high/low richting).
- `UDT_Tank` — sounding/level/volume van één tank + `Array[1..7] of UDT_Alarm`
  (1=HH, 2=H, 3=L, 4=LL, 5=SensorFailure, 6/7=configureerbaar).
- `DB_Tanks` — globale DB, `Array[1..6] of UDT_Tank`.
- `FB_Alarm` — evalueert één alarmkanaal (hysterese, TON-delay, accept-edge).
- `FB_Tank_Alarm_Manager` — roept `Array[1..7] of FB_Alarm` (multi-instance) aan
  per tank; IEC_TIMER kan niet in een UDT zitten, vandaar de multi-instance
  FB-array met de TON's binnenin `FB_Alarm` in plaats van in de UDT.
- `FB_Sensor_Trim` — accumuleert een trim-offset via `R_TRIG`-edges op
  increment/decrement-commando's.
- `udtCurvePoint`, `udtTrimCurve`, `udtTankTable` — sounding→volume
  lookup-tabellen per tank, met trimcorrectie.
- `FB_TankInterpolation` + `FC_InterpolateSoundingCurve` — schaalt de raw
  sounding-waarde naar centimeters (`RawMin/RawMax` → `CmAtRawMin/CmAtRawMax`)
  vóórdat er geïnterpoleerd wordt. Dit lost het eerder gevonden probleem op
  (raw sounding werd niet naar cm geschaald vóór de FB-input).

Zie `Openness/README.md` voor hoe je dit automatisch in TIA Portal opbouwt.
