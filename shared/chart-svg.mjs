/**
 * 由数据点生成 Release 历史折线图 SVG（无依赖，可供浏览器与 Node 复用）
 * points: { x: Date | string | number, countY: number, versionY?: number }[]
 */

function escXml(s) {
  return String(s)
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&apos;");
}

function toTime(p) {
  return new Date(p.x).getTime();
}

export function buildReleaseHistorySvg(points, owner, repo, options = {}) {
  const width = options.width ?? 800;
  const height = options.height ?? 420;
  const pad = { l: 58, r: 62, t: 36, b: 52 };
  const title = options.title ?? `${owner}/${repo} · Release History`;

  if (!points.length) {
    return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
  <rect width="100%" height="100%" fill="#0d1117"/>
  <text x="${width / 2}" y="${height / 2}" fill="#8b949e" font-family="system-ui,sans-serif" font-size="14" text-anchor="middle">${escXml("暂无 Release 数据")}</text>
</svg>`;
  }

  const t0 = toTime(points[0]);
  const t1 = toTime(points[points.length - 1]);
  const tSpan = Math.max(t1 - t0, 1);
  const countMax = Math.max(...points.map((p) => p.countY), 1);

  const verVals = points
    .map((p) => p.versionY)
    .filter((v) => typeof v === "number" && !Number.isNaN(v));
  const hasVersion = verVals.length > 0;
  let vmin = hasVersion ? Math.min(...verVals) : 0;
  let vmax = hasVersion ? Math.max(...verVals) : 1;
  if (hasVersion && vmin === vmax) {
    vmin -= 1;
    vmax += 1;
  }
  const vSpan = Math.max(vmax - vmin, 1);

  const iw = width - pad.l - pad.r;
  const ih = height - pad.t - pad.b;

  const xAt = (t) => pad.l + ((t - t0) / tSpan) * iw;
  const yCount = (c) => pad.t + ih - (c / countMax) * ih;
  const yVer = (v) => pad.t + ih - ((v - vmin) / vSpan) * ih;

  const countPts = points.map((p) => `${xAt(toTime(p)).toFixed(2)},${yCount(p.countY).toFixed(2)}`);
  const countPathD = `M ${countPts.join(" L ")}`;

  const baseY = pad.t + ih;
  const areaD = `${countPathD} L ${xAt(t1).toFixed(2)} ${baseY.toFixed(2)} L ${xAt(t0).toFixed(2)} ${baseY.toFixed(2)} Z`;

  let versionPathD = "";
  if (hasVersion) {
    const vPts = [];
    for (const p of points) {
      if (typeof p.versionY !== "number" || Number.isNaN(p.versionY)) continue;
      vPts.push(`${xAt(toTime(p)).toFixed(2)},${yVer(p.versionY).toFixed(2)}`);
    }
    if (vPts.length) versionPathD = `M ${vPts.join(" L ")}`;
  }

  const gridYs = 4;
  let grid = "";
  for (let g = 0; g <= gridYs; g++) {
    const y = pad.t + (ih * g) / gridYs;
    grid += `<line x1="${pad.l}" y1="${y.toFixed(2)}" x2="${width - pad.r}" y2="${y.toFixed(2)}" stroke="#30363d" stroke-width="1"/>`;
  }

  const tickCount = 4;
  let yLabels = "";
  for (let g = 0; g <= tickCount; g++) {
    const val = Math.round((countMax * (tickCount - g)) / tickCount);
    const y = pad.t + (ih * g) / tickCount;
    yLabels += `<text x="${pad.l - 8}" y="${y + 4}" fill="#8b949e" font-family="system-ui,sans-serif" font-size="11" text-anchor="end">${val}</text>`;
  }

  let yLabelsR = "";
  if (hasVersion) {
    const steps = 4;
    for (let g = 0; g <= steps; g++) {
      const v = vmin + (vSpan * (steps - g)) / steps;
      const y = pad.t + (ih * g) / steps;
      const major = Math.floor(v / 1e6);
      const minor = Math.floor((v % 1e6) / 1e3);
      const patch = Math.round(v % 1e3);
      const lab = `${major}.${minor}.${patch}`;
      yLabelsR += `<text x="${width - pad.r + 8}" y="${y + 4}" fill="#c9a8ff" font-family="system-ui,sans-serif" font-size="11" text-anchor="start">${escXml(lab)}</text>`;
    }
  }

  const d0 = new Date(t0);
  const d1 = new Date(t1);
  const xLabLeft = d0.toISOString().slice(0, 10);
  const xLabRight = d1.toISOString().slice(0, 10);

  const versionStroke = hasVersion && versionPathD
    ? `<path d="${versionPathD}" fill="none" stroke="#a371f7" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round"/>`
    : "";

  return `<?xml version="1.0" encoding="UTF-8"?>
<svg xmlns="http://www.w3.org/2000/svg" width="${width}" height="${height}" viewBox="0 0 ${width} ${height}">
  <rect width="100%" height="100%" fill="#0d1117"/>
  <text x="${width / 2}" y="22" fill="#e6edf3" font-family="system-ui,sans-serif" font-size="15" font-weight="600" text-anchor="middle">${escXml(title)}</text>
  ${grid}
  <path d="${areaD}" fill="rgba(88,166,255,0.15)" stroke="none"/>
  <path d="${countPathD}" fill="none" stroke="#58a6ff" stroke-width="2.5" stroke-linejoin="round" stroke-linecap="round"/>
  ${versionStroke}
  <text x="${pad.l}" y="${height - 18}" fill="#8b949e" font-family="system-ui,sans-serif" font-size="11" text-anchor="start">${escXml(xLabLeft)}</text>
  <text x="${width - pad.r}" y="${height - 18}" fill="#8b949e" font-family="system-ui,sans-serif" font-size="11" text-anchor="end">${escXml(xLabRight)}</text>
  <text x="${pad.l}" y="${height - 4}" fill="#58a6ff" font-family="system-ui,sans-serif" font-size="11" text-anchor="start">累积 Release</text>
  ${hasVersion ? `<text x="${width - pad.r}" y="${height - 4}" fill="#a371f7" font-family="system-ui,sans-serif" font-size="11" text-anchor="end">语义化版本</text>` : ""}
  ${yLabels}
  ${yLabelsR}
</svg>`;
}
