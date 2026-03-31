import { useState } from "react";
import { PieChart, Pie } from "recharts";

// ─── Data ─────────────────────────────────────────────────────────────────────

const salesData = [
  { month: "Sep", sales: 32000, expenses: 18000 },
  { month: "Oct", sales: 38000, expenses: 21000 },
  { month: "Nov", sales: 35000, expenses: 19500 },
  { month: "Dec", sales: 48000, expenses: 24000 },
  { month: "Jan", sales: 42000, expenses: 22000 },
  { month: "Feb", sales: 52000, expenses: 26000 },
];

const revenueData = [
  { name: "Services",      value: 38, color: "#3B82F6" },
  { name: "Products",      value: 27, color: "#10B981" },
  { name: "Consulting",    value: 18, color: "#8B5CF6" },
  { name: "Subscriptions", value: 12, color: "#F59E0B" },
  { name: "Other",         value: 5,  color: "#94A3B8" },
];

// ─── Custom SVG Line Chart ────────────────────────────────────────────────────

function SvgLineChart() {
  const [tooltip, setTooltip] = useState<{ x: number; y: number; idx: number } | null>(null);

  const W = 480;
  const H = 190;
  const padL = 44;
  const padR = 16;
  const padT = 12;
  const padB = 28;
  const chartW = W - padL - padR;
  const chartH = H - padT - padB;

  const maxVal = 55000;
  const yTicks = [0, 10000, 20000, 30000, 40000, 50000];

  const toX = (i: number) => padL + (i / (salesData.length - 1)) * chartW;
  const toY = (v: number) => padT + chartH - (v / maxVal) * chartH;

  const salesPath  = salesData.map((d, i) => `${i === 0 ? "M" : "L"}${toX(i)},${toY(d.sales)}`).join(" ");
  const expPath    = salesData.map((d, i) => `${i === 0 ? "M" : "L"}${toX(i)},${toY(d.expenses)}`).join(" ");

  // Fill area under sales line
  const salesArea = `${salesPath} L${toX(salesData.length - 1)},${padT + chartH} L${toX(0)},${padT + chartH} Z`;

  const hovered = tooltip !== null ? salesData[tooltip.idx] : null;

  return (
    <div style={{ position: "relative", width: "100%", height: H }}>
      <svg
        viewBox={`0 0 ${W} ${H}`}
        width="100%"
        height={H}
        style={{ overflow: "visible", display: "block" }}
      >
        <defs>
          <linearGradient id="salesGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%"   stopColor="#3B82F6" stopOpacity="0.18" />
            <stop offset="100%" stopColor="#3B82F6" stopOpacity="0.01" />
          </linearGradient>
        </defs>

        {/* Y-axis grid lines + labels */}
        {yTicks.map((tick) => {
          const y = toY(tick);
          return (
            <g key={`ytick-${tick}`}>
              <line x1={padL} y1={y} x2={W - padR} y2={y} stroke="#F1F5F9" strokeWidth={1} />
              <text x={padL - 6} y={y + 4} textAnchor="end" fill="#94A3B8" fontSize={9.5}>
                {tick === 0 ? "$0" : `$${tick / 1000}k`}
              </text>
            </g>
          );
        })}

        {/* X-axis labels */}
        {salesData.map((d, i) => (
          <text key={`xlabel-${d.month}`} x={toX(i)} y={H - 6} textAnchor="middle" fill="#94A3B8" fontSize={10}>
            {d.month}
          </text>
        ))}

        {/* Area fill under sales */}
        <path d={salesArea} fill="url(#salesGrad)" />

        {/* Expenses line (dashed) */}
        <path
          d={expPath}
          fill="none"
          stroke="#EF4444"
          strokeWidth={2.5}
          strokeDasharray="6 3"
          strokeLinecap="round"
          strokeLinejoin="round"
        />

        {/* Sales line */}
        <path
          d={salesPath}
          fill="none"
          stroke="#3B82F6"
          strokeWidth={2.5}
          strokeLinecap="round"
          strokeLinejoin="round"
        />

        {/* Hover areas + dots */}
        {salesData.map((d, i) => {
          const cx = toX(i);
          const cySales = toY(d.sales);
          const cyExp   = toY(d.expenses);
          const isHov   = tooltip?.idx === i;
          return (
            <g key={`point-${d.month}`}>
              {/* Invisible wide hit area */}
              <rect
                x={cx - (chartW / (salesData.length - 1)) / 2}
                y={padT}
                width={chartW / (salesData.length - 1)}
                height={chartH}
                fill="transparent"
                onMouseEnter={() => setTooltip({ x: cx, y: Math.min(cySales, cyExp), idx: i })}
                onMouseLeave={() => setTooltip(null)}
                style={{ cursor: "crosshair" }}
              />
              {/* Sales dot */}
              <circle cx={cx} cy={cySales} r={isHov ? 5.5 : 4} fill="#3B82F6" stroke="#fff" strokeWidth={1.5} />
              {/* Expenses dot */}
              <circle cx={cx} cy={cyExp}   r={isHov ? 5.5 : 3.5} fill="#EF4444" stroke="#fff" strokeWidth={1.5} />
              {/* Vertical hover line */}
              {isHov && (
                <line x1={cx} y1={padT} x2={cx} y2={padT + chartH} stroke="#CBD5E1" strokeWidth={1} strokeDasharray="3 3" />
              )}
            </g>
          );
        })}
      </svg>

      {/* Tooltip */}
      {tooltip !== null && hovered && (
        <div
          style={{
            position: "absolute",
            left: `${(toX(tooltip.idx) / W) * 100}%`,
            top: 0,
            transform: "translate(-50%, 4px)",
            backgroundColor: "#1E3A5F",
            borderRadius: 10,
            padding: "8px 12px",
            boxShadow: "0 8px 24px rgba(0,0,0,0.22)",
            fontFamily: "'Inter', sans-serif",
            pointerEvents: "none",
            zIndex: 10,
            whiteSpace: "nowrap",
          }}
        >
          <div style={{ color: "rgba(255,255,255,0.6)", fontSize: 10, marginBottom: 5 }}>{hovered.month}</div>
          <div style={{ display: "flex", alignItems: "center", gap: 6, marginBottom: 3 }}>
            <div style={{ width: 8, height: 8, borderRadius: "50%", backgroundColor: "#3B82F6" }} />
            <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 11 }}>Sales:</span>
            <span style={{ color: "#FFFFFF", fontSize: 11, fontWeight: 700 }}>${hovered.sales.toLocaleString()}</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
            <div style={{ width: 8, height: 8, borderRadius: "50%", backgroundColor: "#EF4444" }} />
            <span style={{ color: "rgba(255,255,255,0.7)", fontSize: 11 }}>Expenses:</span>
            <span style={{ color: "#FFFFFF", fontSize: 11, fontWeight: 700 }}>${hovered.expenses.toLocaleString()}</span>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Main Export ──────────────────────────────────────────────────────────────

export function Charts() {
  const total = revenueData.reduce((a, b) => a + b.value, 0);

  return (
    <div className="flex gap-4">
      {/* ── Custom SVG Line Chart ── */}
      <div
        className="flex-1 rounded-xl p-5"
        style={{
          backgroundColor: "#FFFFFF",
          boxShadow: "0 1px 4px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.04)",
          border: "1px solid #F1F5F9",
          fontFamily: "'Inter', sans-serif",
          minWidth: 0,
        }}
      >
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 style={{ color: "#0F172A", fontSize: "14px", fontWeight: 700, margin: 0 }}>
              Monthly Sales vs Expenses
            </h3>
            <p style={{ color: "#94A3B8", fontSize: "11.5px", margin: "2px 0 0" }}>
              Last 6 months overview
            </p>
          </div>
          <div className="flex items-center gap-3">
            <div className="flex items-center gap-1.5">
              <div style={{ width: "24px", height: "3px", backgroundColor: "#3B82F6", borderRadius: "2px" }} />
              <span style={{ color: "#64748B", fontSize: "11.5px" }}>Sales</span>
            </div>
            <div className="flex items-center gap-1.5">
              <svg width="24" height="3" style={{ flexShrink: 0 }}>
                <line x1="0" y1="1.5" x2="24" y2="1.5" stroke="#EF4444" strokeWidth="2.5" strokeDasharray="5 3" />
              </svg>
              <span style={{ color: "#64748B", fontSize: "11.5px" }}>Expenses</span>
            </div>
            <button
              style={{
                backgroundColor: "#F1F5F9",
                border: "1px solid #E2E8F0",
                borderRadius: "6px",
                padding: "3px 10px",
                color: "#64748B",
                fontSize: "11px",
                cursor: "pointer",
                fontFamily: "'Inter', sans-serif",
              }}
            >
              Monthly ▾
            </button>
          </div>
        </div>

        <SvgLineChart />

        {/* Summary Row */}
        <div className="flex gap-4 mt-3 pt-3" style={{ borderTop: "1px solid #F1F5F9" }}>
          <div className="flex-1 text-center">
            <div style={{ color: "#94A3B8", fontSize: "10.5px", marginBottom: "2px" }}>Total Sales</div>
            <div style={{ color: "#10B981", fontSize: "14px", fontWeight: 700 }}>$247,000</div>
          </div>
          <div style={{ width: "1px", backgroundColor: "#F1F5F9" }} />
          <div className="flex-1 text-center">
            <div style={{ color: "#94A3B8", fontSize: "10.5px", marginBottom: "2px" }}>Total Expenses</div>
            <div style={{ color: "#EF4444", fontSize: "14px", fontWeight: 700 }}>$130,500</div>
          </div>
          <div style={{ width: "1px", backgroundColor: "#F1F5F9" }} />
          <div className="flex-1 text-center">
            <div style={{ color: "#94A3B8", fontSize: "10.5px", marginBottom: "2px" }}>Net Profit</div>
            <div style={{ color: "#1E3A5F", fontSize: "14px", fontWeight: 700 }}>$116,500</div>
          </div>
        </div>
      </div>

      {/* ── Donut Chart (Recharts PieChart — single Pie, no Tooltip, no Cell) ── */}
      <div
        className="rounded-xl p-5"
        style={{
          width: "320px",
          minWidth: "320px",
          backgroundColor: "#FFFFFF",
          boxShadow: "0 1px 4px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.04)",
          border: "1px solid #F1F5F9",
          fontFamily: "'Inter', sans-serif",
        }}
      >
        <div className="flex items-center justify-between mb-3">
          <div>
            <h3 style={{ color: "#0F172A", fontSize: "14px", fontWeight: 700, margin: 0 }}>
              Revenue by Category
            </h3>
            <p style={{ color: "#94A3B8", fontSize: "11.5px", margin: "2px 0 0" }}>
              Feb 2026
            </p>
          </div>
        </div>

        <div className="flex items-center gap-4">
          <div style={{ position: "relative", flexShrink: 0 }}>
            <PieChart width={140} height={140}>
              <Pie
                data={revenueData.map(d => ({ ...d, fill: d.color }))}
                cx="50%"
                cy="50%"
                innerRadius={46}
                outerRadius={66}
                paddingAngle={3}
                dataKey="value"
                stroke="none"
                isAnimationActive={false}
              />
            </PieChart>
            {/* Center label */}
            <div
              style={{
                position: "absolute",
                top: "50%",
                left: "50%",
                transform: "translate(-50%, -50%)",
                textAlign: "center",
                pointerEvents: "none",
              }}
            >
              <div style={{ color: "#0F172A", fontSize: "16px", fontWeight: 800, lineHeight: 1 }}>
                $52k
              </div>
              <div style={{ color: "#94A3B8", fontSize: "9px", fontWeight: 500, marginTop: "2px" }}>
                Total
              </div>
            </div>
          </div>

          {/* Legend */}
          <div className="flex flex-col gap-2.5 flex-1">
            {revenueData.map((item) => (
              <div key={item.name} className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <div
                    style={{
                      width: "10px",
                      height: "10px",
                      borderRadius: "3px",
                      backgroundColor: item.color,
                      flexShrink: 0,
                    }}
                  />
                  <span style={{ color: "#64748B", fontSize: "12px" }}>{item.name}</span>
                </div>
                <div className="flex items-center gap-2">
                  <div
                    style={{
                      height: "4px",
                      width: "40px",
                      borderRadius: "2px",
                      backgroundColor: "#F1F5F9",
                      overflow: "hidden",
                    }}
                  >
                    <div
                      style={{
                        height: "100%",
                        width: `${(item.value / total) * 100}%`,
                        backgroundColor: item.color,
                        borderRadius: "2px",
                      }}
                    />
                  </div>
                  <span style={{ color: "#0F172A", fontSize: "12px", fontWeight: 700, minWidth: "28px", textAlign: "right" }}>
                    {item.value}%
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Growth note */}
        <div
          className="flex items-center gap-2 px-3 py-2 rounded-lg mt-3"
          style={{ backgroundColor: "#ECFDF5", border: "1px solid #A7F3D0" }}
        >
          <div style={{ width: "6px", height: "6px", borderRadius: "50%", backgroundColor: "#10B981", flexShrink: 0 }} />
          <span style={{ color: "#065F46", fontSize: "11.5px", fontWeight: 500 }}>
            Services revenue grew <strong>+18.4%</strong> vs last month
          </span>
        </div>
      </div>
    </div>
  );
}
