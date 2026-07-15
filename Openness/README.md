# TiaOpennessBuilder

C#-consoletoepassing die het TIA Portal V20-project automatisch opbouwt via de
**Openness API**: PLC-software openen/aanmaken, de SCL-bronnen in `PLC/SCL`
importeren ("Generate blocks from source"), `DB_Tanks` vullen vanuit config,
een tag-tabel aanmaken en (best-effort) lege HMI-schermen scaffolden.

## Belangrijk: lokaal uitvoeren tegen TIA Portal

Dit script moet **lokaal** draaien op een machine met TIA Portal V20 +
Openness-optie geïnstalleerd. Er is vanuit deze omgeving geen netwerktoegang
tot TIA Portal, dus dit is niet hier te testen — controleer de API-aanroepen
tegen de daadwerkelijke `Siemens.Engineering.dll` (via IntelliSense/Object
Browser in Visual Studio) voordat je het script op een productieproject
loslaat, en run eerst tegen een wegwerp-testproject.

## Vereisten

- TIA Portal V20 met de Openness-optie geïnstalleerd.
- .NET Framework 4.8 SDK / Visual Studio 2022 (of `dotnet build` met de juiste
  workload) — target platform **x64**, moet overeenkomen met TIA Portal.
- De Openness-DLL's worden **niet** meegeleverd in deze repo (Siemens-eigendom).
  Ze staan normaal op:
  `C:\Program Files\Siemens\Automation\Portal V20\PublicAPI\V20\Siemens.Engineering.dll`
  Wijkt jouw installatiepad af, bouw dan met:
  ```
  dotnet build -p:TiaPortalInstallDir="D:\Siemens\Portal V20"
  ```
- Windows: registreer/vertrouw de Openness-toegang bij de eerste run
  (TIA Portal vraagt hiervoor een bevestiging als je met UI draait).

## Config

Kopieer `project-config.sample.json` en pas aan:

- `AttachToRunningInstance` — zet op `true` om je **al open** TIA Portal
  (met het project geladen) te hergebruiken in plaats van er zelf een
  nieuwe kopie van te openen. Je hoeft het project dan niet meer te sluiten
  voor elke run. Vereist wel dat TIA Portal al open staat mét het project
  geladen vóór je het script start; `CreateNewProject`/`TiaProjectPath`
  worden in dat geval genegeerd (alleen `TiaProjectName` wordt gebruikt om
  het juiste open project te vinden als er meerdere open staan). Staat dit
  op `false` (standaard), dan opent het script zelf een verse kopie van het
  project en moet dat project dus gesloten zijn in de GUI.
- `CreateNewProject` / `TiaProjectDirectory` / `TiaProjectName` voor een
  nieuw project, of `TiaProjectPath` om een bestaand `.ap20`-project te openen.
- `PlcOrderNumber` — exacte catalogusstring van je S7-1500 CPU, bv.
  `"OrderNumber:6ES7 515-2AM02-0AB0/V3.0"`. Zoek de exacte string op via
  Hardwarecatalogus in TIA Portal (rechtsklik → "Properties" op een CPU) of
  in de HSP-documentatie — pas aan naar jouw exacte CPU-type/firmwareversie.
- `HmiOrderNumber` — voor de MTP1200 (SIMATIC HMI Unified Comfort Panel 12″),
  standaard `"OrderNumber:6AV2128-3XB06-0AX1"`. **Controleer dit tegen de
  Hardwarecatalogus in jouw TIA Portal V20** (Unified Comfort Panels hebben
  meerdere varianten/revisies) voordat je het script tegen een echt project
  draait — een onjuiste catalogusstring laat `CreateWithItem` falen.
- `SclSourceDirectory` + `SclImportOrder` — de SCL-bestanden uit `PLC/SCL`
  die in deze volgorde geïmporteerd worden (UDT's vóór de FB's die ze
  gebruiken). `DB_Tanks` staat hier bewust *niet* in: die wordt dynamisch
  gegenereerd uit de `Tanks`-sectie van de config en apart geïmporteerd
  (zie `TankDbGenerator`).
- `Tanks` — 6 tanks met sounding-scaling (`RawMin/RawMax` → `CmAtRawMin/CmAtRawMax`,
  gebruikt door `FB_TankInterpolation`) en per-tank alarm-setpoints
  (kanaal 1..7 = HH, H, L, LL, SensorFailure, Spare1, Spare2).

## Runnen

```
cd Openness/src/TiaOpennessBuilder
dotnet build -c Release
.\bin\Release\net48\TiaOpennessBuilder.exe ..\..\project-config.json
```

net48 is klassiek .NET Framework: de `.exe` wordt rechtstreeks uitgevoerd, niet via
`dotnet <dll>` (dat is alleen voor .NET-Core/.NET-5+ assemblies).

Het script is idempotent voor de PLC-kant: opnieuw draaien importeert de
bronnen opnieuw (bestaande external sources met dezelfde naam worden eerst
verwijderd) en slaat het project op.

## Bekende beperkingen

- **`PlcExternalSourceComposition.CreateFromFile` is op minstens één getest
  systeem geblokkeerd** met `"The method is not supported by the current
  version"`, zelfs met correcte SCL-inhoud en een correcte projectstatus.
  Waarschijnlijk een licentie-/editiebeperking op scriptend schrijven naar
  external sources (bevestigd doordat exact dezelfde actie via de TIA
  Portal-GUI — Add new external file → Generate blocks from source — wél
  altijd werkte). Zet in dat geval `"ImportPlcSources": false` in je config
  en importeer de bestanden uit `PLC/SCL` handmatig via de GUI; het script
  doet dan alleen nog de tag-tabel en (optioneel) de HMI-schermen.
- **HMI-schermen**: de publieke Openness API biedt voor WinCC Comfort/Advanced
  nauwelijks toegang tot losse grafische objecten (IO-fields, de
  tankniveau-gauge, HH/H/L/LL-markers, faceplate-instanties). `HmiScreenBuilder`
  maakt daarom alleen lege schermen per tank aan (`Tank1_Overview`, …). Bouw
  de daadwerkelijke schermindeling één keer met de hand (of als
  library-mastercopy) en kopieer die per tank, of breid deze klasse uit nadat
  je hebt geverifieerd welke `Hmi.Screen`-APIs jouw Openness-versie echt
  ondersteunt.
- **Trim-interpolatie**: `FB_TankInterpolation` interpoleert momenteel alleen
  op de nul-trim curve; de blend tussen de dichtstbijzijnde `TrimCurves` op
  basis van `TrimAngleDeg` staat als TODO in `PLC/SCL/FB_TankInterpolation.scl`.
- **API-versieverschillen**: methodesignaturen (bv. `ExternalSource.Delete()`,
  `GenerateBlocksFromSource()`) kunnen licht verschillen tussen Openness
  V19/V20. Bouw eerst met de daadwerkelijke DLL's als referentie en corrigeer
  waar nodig.
