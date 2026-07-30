using InheritanceSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace inheritance_system.Services
{
    // ── DTOs ─────────────────────────────────────────────
    public class FaraidShare
    {
        public int HeirId { get; set; }
        public string HeirName { get; set; } = "";
        public string Relation { get; set; } = "";
        public string FractionLabel { get; set; } = "";
        public decimal Percent { get; set; }
        public decimal Amount { get; set; }
        public bool IsResiduary { get; set; }
        public bool IsExcluded { get; set; }
        public string ExclusionReason { get; set; } = "";
    }

    public class FaraidResult
    {
        public int PropertyId { get; set; }
        public string PropertyTitle { get; set; } = "";
        public decimal EstimatedValue { get; set; }
        public List<FaraidShare> Shares { get; set; } = new();
        public List<string> Notes { get; set; } = new();
        public bool AwlApplied { get; set; }
        public bool RaddApplied { get; set; }
        public decimal TotalPercent { get; set; }
    }

    public class FaraidService
    {
        private readonly AppDbContext _db;
        public FaraidService(AppDbContext db) { _db = db; }

        // Compute Sharia shares for a property
        public async Task<FaraidResult?> ComputeAsync(int propertyId, int ownerId)
        {
            var prop = await _db.Properties
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.OwnerId == ownerId);
            if (prop == null) return null;

            var heirs = await _db.Heirs
                .Where(h => h.PropertyId == propertyId)
                .OrderBy(h => h.HeirId)
                .ToListAsync();

            var result = new FaraidResult
            {
                PropertyId = prop.PropertyId,
                PropertyTitle = prop.Title,
                EstimatedValue = prop.EstimatedValue
            };

            if (heirs.Count == 0)
            {
                result.Notes.Add("No heirs registered for this property. Add heirs via Heir Management first.");
                return result;
            }

            // Group heirs by relation (normalized)
            var byRel = heirs.GroupBy(h => (h.Relation ?? "").Trim())
                             .ToDictionary(g => g.Key, g => g.ToList());

            int CountOf(params string[] keys) =>
                keys.Sum(k => byRel.TryGetValue(k, out var l) ? l.Count : 0);

            // Heir counts
            int sons = CountOf("Son");
            int daughters = CountOf("Daughter");
            int sonsSon = CountOf("Son's Son");
            int sonsDaughter = CountOf("Son's Daughter");
            int husband = CountOf("Husband");
            int wives = CountOf("Wife");
            int father = CountOf("Father");
            int mother = CountOf("Mother");
            int pGF = CountOf("Paternal Grandfather");
            int pGM = CountOf("Paternal Grandmother");
            int mGM = CountOf("Maternal Grandmother");
            int fullBros = CountOf("Full Brother");
            int fullSis = CountOf("Full Sister");
            int patBros = CountOf("Paternal Half Brother");
            int patSis = CountOf("Paternal Half Sister");
            int matBros = CountOf("Maternal Half Brother");
            int matSis = CountOf("Maternal Half Sister");

            bool hasMaleDescendant = sons > 0 || sonsSon > 0;
            bool hasAnyDescendant = sons > 0 || daughters > 0 || sonsSon > 0 || sonsDaughter > 0;
            bool hasFatherOrPGF = father > 0 || pGF > 0;
            bool siblingsCount = (fullBros + fullSis + patBros + patSis + matBros + matSis) >= 2;

            // Fixed shares (as fraction of 1)
            var shareMap = new Dictionary<string, (decimal frac, string label)>();
            var residuaries = new List<string>();
            var excluded = new Dictionary<string, string>();

            // ─── SPOUSE ───
            if (husband > 0)
                shareMap["Husband"] = hasAnyDescendant ? (1m / 4m, "1/4") : (1m / 2m, "1/2");
            if (wives > 0)
                shareMap["Wife"] = hasAnyDescendant ? (1m / 8m, "1/8") : (1m / 4m, "1/4");

            // ─── FATHER ───
            if (father > 0)
            {
                if (hasMaleDescendant) shareMap["Father"] = (1m / 6m, "1/6");
                else if (daughters > 0 || sonsDaughter > 0)
                    shareMap["Father"] = (1m / 6m, "1/6 + Residue");
                else residuaries.Add("Father");
                excluded["Paternal Grandfather"] = "Excluded by Father";
            }

            // ─── PATERNAL GRANDFATHER (only if no father) ───
            if (pGF > 0 && father == 0)
            {
                if (hasMaleDescendant) shareMap["Paternal Grandfather"] = (1m / 6m, "1/6");
                else if (daughters > 0 || sonsDaughter > 0)
                    shareMap["Paternal Grandfather"] = (1m / 6m, "1/6 + Residue");
                else residuaries.Add("Paternal Grandfather");
            }

            // ─── MOTHER ───
            if (mother > 0)
            {
                shareMap["Mother"] = (hasAnyDescendant || siblingsCount) ? (1m / 6m, "1/6") : (1m / 3m, "1/3");
                excluded["Paternal Grandmother"] = "Excluded by Mother";
                excluded["Maternal Grandmother"] = "Excluded by Mother";
            }

            // ─── GRANDMOTHERS (if mother absent) ───
            if (mother == 0)
            {
                if (pGM > 0 && father == 0) shareMap["Paternal Grandmother"] = (1m / 6m, "1/6");
                else if (pGM > 0) excluded["Paternal Grandmother"] = "Excluded by Father";
                if (mGM > 0) shareMap["Maternal Grandmother"] = (1m / 6m, "1/6");
                // If both grandmothers, they share the 1/6 equally — handled later in distribution
            }

            // ─── DAUGHTERS / SONS ───
            if (sons > 0)
            {
                residuaries.Add("Son");                                  // sons are residuary
                if (daughters > 0) residuaries.Add("Daughter");          // share with sons 2:1
            }
            else if (daughters > 0)
            {
                shareMap["Daughter"] = daughters == 1 ? (1m / 2m, "1/2") : (2m / 3m, "2/3");
            }

            // ─── SON'S CHILDREN (only if no son) ───
            if (sons == 0)
            {
                if (sonsSon > 0)
                {
                    residuaries.Add("Son's Son");
                    if (sonsDaughter > 0) residuaries.Add("Son's Daughter");
                }
                else if (sonsDaughter > 0)
                {
                    if (daughters == 0)
                        shareMap["Son's Daughter"] = sonsDaughter == 1 ? (1m / 2m, "1/2") : (2m / 3m, "2/3");
                    else if (daughters == 1)
                        shareMap["Son's Daughter"] = (1m / 6m, "1/6 (with one daughter)");
                    else
                        excluded["Son's Daughter"] = "Excluded by 2+ daughters";
                }
            }
            else
            {
                if (sonsSon > 0) excluded["Son's Son"] = "Excluded by Son";
                if (sonsDaughter > 0) excluded["Son's Daughter"] = "Excluded by Son";
            }

            // ─── SIBLINGS (excluded by descendants or father/PGF) ───
            bool siblingsExcluded = hasMaleDescendant || hasFatherOrPGF;
            if (siblingsExcluded)
            {
                foreach (var r in new[] { "Full Brother","Full Sister","Paternal Half Brother",
                                          "Paternal Half Sister","Maternal Half Brother","Maternal Half Sister" })
                    if (byRel.ContainsKey(r)) excluded[r] = "Excluded by son/grandson/father";
            }
            else
            {
                // Maternal half-siblings: also excluded by any descendant (incl. daughters)
                if (hasAnyDescendant)
                {
                    if (matBros > 0) excluded["Maternal Half Brother"] = "Excluded by descendant";
                    if (matSis > 0) excluded["Maternal Half Sister"] = "Excluded by descendant";
                }
                else
                {
                    int matTotal = matBros + matSis;
                    if (matTotal == 1)
                    {
                        if (matBros == 1) shareMap["Maternal Half Brother"] = (1m / 6m, "1/6");
                        else shareMap["Maternal Half Sister"] = (1m / 6m, "1/6");
                    }
                    else if (matTotal >= 2)
                    {
                        // 1/3 split equally between maternal half-siblings — handled in distribution
                        if (matBros > 0) shareMap["Maternal Half Brother"] = ((decimal)matBros / matTotal * (1m / 3m), "1/3 shared");
                        if (matSis > 0) shareMap["Maternal Half Sister"] = ((decimal)matSis / matTotal * (1m / 3m), "1/3 shared");
                    }
                }

                // Full siblings
                if (fullBros > 0)
                {
                    residuaries.Add("Full Brother");
                    if (fullSis > 0) residuaries.Add("Full Sister");
                    if (patBros > 0) excluded["Paternal Half Brother"] = "Excluded by Full Brother";
                    if (patSis > 0) excluded["Paternal Half Sister"] = "Excluded by Full Brother";
                }
                else if (fullSis > 0)
                {
                    // With daughters present → full sisters become residuaries (Asabah ma'a ghair)
                    if (daughters > 0 || sonsDaughter > 0)
                    {
                        residuaries.Add("Full Sister");
                        if (patBros > 0) excluded["Paternal Half Brother"] = "Excluded by Full Sister as residuary";
                        if (patSis > 0) excluded["Paternal Half Sister"] = "Excluded by Full Sister as residuary";
                    }
                    else
                    {
                        shareMap["Full Sister"] = fullSis == 1 ? (1m / 2m, "1/2") : (2m / 3m, "2/3");
                        if (patBros == 0 && patSis > 0)
                            shareMap["Paternal Half Sister"] = fullSis == 1 ? (1m / 6m, "1/6 complement") : (0m, "Excluded");
                        if (patBros > 0)
                        {
                            residuaries.Add("Paternal Half Brother");
                            if (patSis > 0) residuaries.Add("Paternal Half Sister");
                        }
                    }
                }
                else // no full siblings
                {
                    if (patBros > 0)
                    {
                        residuaries.Add("Paternal Half Brother");
                        if (patSis > 0) residuaries.Add("Paternal Half Sister");
                    }
                    else if (patSis > 0)
                    {
                        if (daughters > 0 || sonsDaughter > 0)
                            residuaries.Add("Paternal Half Sister");
                        else
                            shareMap["Paternal Half Sister"] = patSis == 1 ? (1m / 2m, "1/2") : (2m / 3m, "2/3");
                    }
                }
            }

            // ─── Sum of fixed shares ───
            decimal fixedSum = shareMap.Values.Sum(v => v.frac);

            // ─── AWL: if fixedSum > 1, reduce proportionally ───
            if (fixedSum > 1m)
            {
                result.AwlApplied = true;
                decimal factor = 1m / fixedSum;
                var keys = shareMap.Keys.ToList();
                foreach (var k in keys)
                    shareMap[k] = (shareMap[k].frac * factor, shareMap[k].label + " (Awl)");
                fixedSum = 1m;
                result.Notes.Add("Awl applied: total exceeded 1, shares reduced proportionally.");
            }

            // ─── RESIDUE ───
            decimal residue = 1m - fixedSum;
            var residueShares = new Dictionary<string, decimal>();

            if (residuaries.Count > 0 && residue > 0)
            {
                // Sons + Daughters share residue 2:1
                if (residuaries.Contains("Son"))
                {
                    int units = sons * 2 + daughters;
                    decimal perUnit = residue / units;
                    residueShares["Son"] = perUnit * 2 * sons;
                    if (daughters > 0) residueShares["Daughter"] = perUnit * daughters;
                }
                else if (residuaries.Contains("Son's Son"))
                {
                    int units = sonsSon * 2 + sonsDaughter;
                    decimal perUnit = residue / units;
                    residueShares["Son's Son"] = perUnit * 2 * sonsSon;
                    if (sonsDaughter > 0) residueShares["Son's Daughter"] = perUnit * sonsDaughter;
                }
                else if (residuaries.Contains("Full Brother"))
                {
                    int units = fullBros * 2 + fullSis;
                    decimal perUnit = residue / units;
                    residueShares["Full Brother"] = perUnit * 2 * fullBros;
                    if (fullSis > 0) residueShares["Full Sister"] = perUnit * fullSis;
                }
                else if (residuaries.Contains("Full Sister"))
                {
                    residueShares["Full Sister"] = residue;
                }
                else if (residuaries.Contains("Paternal Half Brother"))
                {
                    int units = patBros * 2 + patSis;
                    decimal perUnit = residue / units;
                    residueShares["Paternal Half Brother"] = perUnit * 2 * patBros;
                    if (patSis > 0) residueShares["Paternal Half Sister"] = perUnit * patSis;
                }
                else if (residuaries.Contains("Paternal Half Sister"))
                {
                    residueShares["Paternal Half Sister"] = residue;
                }
                else if (residuaries.Contains("Father"))
                {
                    residueShares["Father"] = residue;
                }
                else if (residuaries.Contains("Paternal Grandfather"))
                {
                    residueShares["Paternal Grandfather"] = residue;
                }
            }
            else if (residue > 0 && shareMap.Count > 0)
            {
                // ─── RADD: no residuary → return surplus to fard heirs proportionally
                //          (spouse excluded from radd)
                var raddKeys = shareMap.Keys.Where(k => k != "Husband" && k != "Wife").ToList();
                decimal raddBase = raddKeys.Sum(k => shareMap[k].frac);
                if (raddBase > 0)
                {
                    result.RaddApplied = true;
                    foreach (var k in raddKeys)
                        shareMap[k] = (shareMap[k].frac + (residue * shareMap[k].frac / raddBase),
                                       shareMap[k].label + " (Radd)");
                    result.Notes.Add("Radd applied: residue returned proportionally (spouse excluded).");
                }
            }

            // ─── Distribute group shares to individual heirs ───
            foreach (var h in heirs)
            {
                var rel = (h.Relation ?? "").Trim();
                var share = new FaraidShare
                {
                    HeirId = h.HeirId,
                    HeirName = h.FullName,
                    Relation = rel
                };

                if (excluded.ContainsKey(rel))
                {
                    share.IsExcluded = true;
                    share.ExclusionReason = excluded[rel];
                    share.FractionLabel = "Excluded";
                    share.Percent = 0m;
                }
                else if (shareMap.ContainsKey(rel))
                {
                    int groupCount = byRel[rel].Count;
                    decimal groupShare = shareMap[rel].frac;
                    share.FractionLabel = shareMap[rel].label;
                    share.Percent = (groupShare / groupCount) * 100m;
                }
                else if (residueShares.ContainsKey(rel))
                {
                    int groupCount = byRel[rel].Count;
                    decimal groupShare = residueShares[rel];
                    share.IsResiduary = true;
                    share.FractionLabel = "Residue (Asabah)";
                    share.Percent = (groupShare / groupCount) * 100m;
                }
                else
                {
                    share.FractionLabel = "—";
                    share.Percent = 0m;
                    share.IsExcluded = true;
                    share.ExclusionReason = "Not recognised as a Quranic/Asabah heir for this case";
                }

                share.Amount = (share.Percent / 100m) * prop.EstimatedValue;
                result.Shares.Add(share);
            }

            result.TotalPercent = result.Shares.Sum(s => s.Percent);
            return result;
        }

        // Write the computed shares back to the Heirs.SharePercent column
        public async Task<(bool ok, string msg)> ApplyToHeirsAsync(int propertyId, int ownerId)
        {
            var res = await ComputeAsync(propertyId, ownerId);
            if (res == null) return (false, "Property not found.");
            if (res.Shares.Count == 0) return (false, "No heirs to update.");

            foreach (var s in res.Shares)
            {
                var heir = await _db.Heirs.FirstOrDefaultAsync(h => h.HeirId == s.HeirId);
                if (heir != null) heir.SharePercent = Math.Round(s.Percent, 4);
            }
            await _db.SaveChangesAsync();
            return (true, "Sharia shares applied to all heirs of this property.");
        }
    }
}
