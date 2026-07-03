# Vocabulary Reconcile

Small C# console tool for reconciling from source to destination:

- Source: `Chinese Material.xlsx`
- Destination: `Chinese Phrases.xlsm`

The program reads the source vocabulary from `Chinese Material.xlsx` sheet `Traverse`, then compares its `Chinese` values with the staged output workbook if it already exists. If the staged output workbook does not exist yet, it compares against `Chinese Phrases.xlsm` sheet `vocabulary`.

If missing rows are found, the program writes them to the staged macro-enabled workbook. The staged workbook is then published as `Chinese Phrases.xlsm`: the previous live workbook is archived beside it as the next available `Chinese Phrases vNNN.xlsm`.

If no rows are missing and no staged output workbook exists, it prints `Updated rows: 0` and does not create, archive, or overwrite a workbook. If a staged output workbook already exists, it is still published.

File open, copy, replace, and revision-scan operations are retried to tolerate short Dropbox sync locks.

## Run

From this folder:

```bash
dotnet run
```

Or pass explicit workbook paths and output path:

```bash
dotnet run -- "Chinese Material.xlsx" "Chinese Phrases.xlsm" "Updated.xlsm"
```
