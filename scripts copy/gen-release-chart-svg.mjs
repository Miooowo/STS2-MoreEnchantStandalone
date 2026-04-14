#!/usr/bin/env node
/**
 * 拉取 GitHub Releases 并写入 SVG（供 README 引用或 CI 提交）
 * 用法: node scripts/gen-release-chart-svg.mjs <owner> <repo> [输出路径]
 * 环境: GITHUB_TOKEN 可选，提高 API 限额
 */
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { buildReleaseHistorySvg } from "../shared/chart-svg.mjs";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

async function fetchAllReleases(owner, repo, token) {
  const releases = [];
  let page = 1;
  const perPage = 100;
  const headers = {
    Accept: "application/vnd.github+json",
    "X-GitHub-Api-Version": "2022-11-28",
  };
  if (token) headers.Authorization = `Bearer ${token}`;

  while (true) {
    const url = `https://api.github.com/repos/${encodeURIComponent(owner)}/${encodeURIComponent(repo)}/releases?per_page=${perPage}&page=${page}`;
    const res = await fetch(url, { headers });
    if (!res.ok) {
      const text = await res.text();
      throw new Error(`GitHub API ${res.status}: ${text}`);
    }
    const batch = await res.json();
    if (!Array.isArray(batch) || batch.length === 0) break;
    for (const r of batch) {
      if (r.draft || !r.published_at) continue;
      releases.push(r);
    }
    if (batch.length < perPage) break;
    page += 1;
  }
  return releases;
}

function semverParts(major, minor, patch) {
  return {
    num: major * 1_000_000 + minor * 1_000 + patch,
    str: `${major}.${minor}.${patch}`,
  };
}

function parseSemver(tag) {
  if (!tag || typeof tag !== "string") return null;
  const raw = tag.trim().replace(/^v/i, "");
  let m = raw.match(/^(\d+)\.(\d+)\.(\d+)/);
  if (m) return semverParts(+m[1], +m[2], +m[3]);
  m = raw.match(/^(\d+)\.(\d+)(?:[^.\d]|$)/);
  if (m) return semverParts(+m[1], +m[2], 0);
  return null;
}

function buildSeries(releases) {
  const sorted = [...releases].sort(
    (a, b) => new Date(a.published_at) - new Date(b.published_at)
  );
  let lastSemver = null;
  return sorted.map((r, i) => {
    const tag = r.tag_name || r.name || `#${i + 1}`;
    const sv = parseSemver(tag);
    if (sv) lastSemver = sv;
    return {
      x: new Date(r.published_at),
      countY: i + 1,
      versionY: lastSemver ? lastSemver.num : NaN,
    };
  });
}

const owner = process.argv[2];
const repo = process.argv[3];
const outArg = process.argv[4];
const outPath = path.resolve(
  __dirname,
  "..",
  outArg || "docs/release-chart.svg"
);

if (!owner || !repo) {
  console.error(
    "用法: node scripts/gen-release-chart-svg.mjs <owner> <repo> [输出路径]\n" +
      "示例: node scripts/gen-release-chart-svg.mjs Miooowo STS2-MoreEnchantStandalone docs/release-chart.svg"
  );
  process.exit(1);
}

const token = process.env.GITHUB_TOKEN || process.env.GH_TOKEN || "";

try {
  const releases = await fetchAllReleases(owner, repo, token);
  const points = buildSeries(releases);
  const svg = buildReleaseHistorySvg(points, owner, repo);
  fs.mkdirSync(path.dirname(outPath), { recursive: true });
  fs.writeFileSync(outPath, svg, "utf8");
  console.log(`已写入 ${outPath}（${points.length} 个 Release）`);
} catch (e) {
  console.error(e.message || e);
  process.exit(1);
}
