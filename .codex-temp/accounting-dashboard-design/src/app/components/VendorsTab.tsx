import React, { useState } from "react";
import {
  Search, Plus, Download, Mail, Phone, Building2, Package,
  ChevronRight, Star, ArrowUpRight, ExternalLink, ShoppingCart,
  TrendingUp, Clock, AlertCircle, CheckCircle, Users,
} from "lucide-react";

// ─── Types ─────────────────────────────────────────────────────────────────

type VendorStatus = "active" | "inactive";

interface LinkedItem {
  sku: string;
  name: string;
  inStock: number;
  reorderLevel: number;
  unit: string;
  status: "in-stock" | "low-stock" | "out-of-stock" | "overstocked";
}

interface PurchaseOrder {
  id: string;
  date: string;
  amount: number;
  status: "received" | "pending" | "partial";
}

interface Vendor {
  id: string;
  name: string;
  contactName: string;
  email: string;
  phone: string;
  country: string;
  categories: string[];
  paymentTerms: string;
  status: VendorStatus;
  totalOrders: number;
  totalValue: number;
  outstanding: number;
  leadTimeDays: number;
  rating: number; // 1-5
  since: string;
  linkedItems: LinkedItem[];
  monthlySpend: { month: string; value: number }[];
  recentPOs: PurchaseOrder[];
}

// ─── Data ──────────────────────────────────────────────────────────────────

const VENDORS: Vendor[] = [
  {
    id: "V-001",
    name: "TechCorp Solutions",
    contactName: "Khalid Al-Rashid",
    email: "khalid@techcorp.sa",
    phone: "+966 11 234 5678",
    country: "Saudi Arabia",
    categories: ["Electronics"],
    paymentTerms: "Net 30",
    status: "active",
    totalOrders: 24,
    totalValue: 87400,
    outstanding: 12800,
    leadTimeDays: 5,
    rating: 4.8,
    since: "Mar 2023",
    linkedItems: [
      { sku: "EL-1001", name: "Laptop — Dell Inspiron 15",   inStock: 24, reorderLevel: 10, unit: "pcs", status: "in-stock"   },
      { sku: "EL-1002", name: "Wireless Mouse — Logitech MX", inStock: 5,  reorderLevel: 20, unit: "pcs", status: "low-stock"  },
      { sku: "EL-1006", name: "Webcam — HD 1080p",            inStock: 17, reorderLevel: 12, unit: "pcs", status: "in-stock"   },
    ],
    monthlySpend: [
      { month: "Oct", value: 8200 }, { month: "Nov", value: 6500 }, { month: "Dec", value: 9100 },
      { month: "Jan", value: 7800 }, { month: "Feb", value: 11200 }, { month: "Mar", value: 8400 },
    ],
    recentPOs: [
      { id: "PO-2026-045", date: "Feb 22, 2026", amount: 11200, status: "received" },
      { id: "PO-2026-031", date: "Feb 05, 2026", amount: 8400,  status: "received" },
      { id: "PO-2026-058", date: "Mar 10, 2026", amount: 12800, status: "pending"  },
    ],
  },
  {
    id: "V-002",
    name: "NextGen Supplies",
    contactName: "Fatima Zahra",
    email: "fatima@nextgen.com",
    phone: "+971 4 567 8901",
    country: "UAE",
    categories: ["Electronics", "Raw Materials"],
    paymentTerms: "Net 45",
    status: "active",
    totalOrders: 18,
    totalValue: 34200,
    outstanding: 6500,
    leadTimeDays: 8,
    rating: 4.2,
    since: "Aug 2023",
    linkedItems: [
      { sku: "EL-1003", name: "USB-C Hub — 7-in-1",       inStock: 0,  reorderLevel: 15, unit: "pcs", status: "out-of-stock" },
      { sku: "EL-1005", name: "Mechanical Keyboard",       inStock: 3,  reorderLevel: 10, unit: "pcs", status: "low-stock"   },
      { sku: "RM-3004", name: "Industrial Adhesive — 5L",  inStock: 28, reorderLevel: 20, unit: "can", status: "in-stock"    },
    ],
    monthlySpend: [
      { month: "Oct", value: 3200 }, { month: "Nov", value: 4100 }, { month: "Dec", value: 2900 },
      { month: "Jan", value: 5200 }, { month: "Feb", value: 3800 }, { month: "Mar", value: 4400 },
    ],
    recentPOs: [
      { id: "PO-2026-040", date: "Feb 15, 2026", amount: 3800, status: "received" },
      { id: "PO-2026-055", date: "Mar 08, 2026", amount: 6500, status: "partial"  },
    ],
  },
  {
    id: "V-003",
    name: "Global Imports LLC",
    contactName: "James Mitchell",
    email: "james@globalimports.ae",
    phone: "+971 4 890 1234",
    country: "UAE",
    categories: ["Electronics", "Raw Materials"],
    paymentTerms: "Net 60",
    status: "active",
    totalOrders: 31,
    totalValue: 142600,
    outstanding: 28400,
    leadTimeDays: 14,
    rating: 4.5,
    since: "Jan 2022",
    linkedItems: [
      { sku: "EL-1004", name: "Monitor — 27\" 4K IPS",    inStock: 88,  reorderLevel: 8,   unit: "pcs",  status: "overstocked" },
      { sku: "RM-3001", name: "Aluminium Sheet — 2mm",     inStock: 640, reorderLevel: 200, unit: "kg",   status: "overstocked" },
      { sku: "RM-3002", name: "PVC Granules — Grade A",    inStock: 95,  reorderLevel: 150, unit: "kg",   status: "low-stock"   },
      { sku: "RM-3005", name: "Copper Wire — 2.5mm²",      inStock: 180, reorderLevel: 50,  unit: "roll", status: "overstocked" },
    ],
    monthlySpend: [
      { month: "Oct", value: 18000 }, { month: "Nov", value: 22000 }, { month: "Dec", value: 15000 },
      { month: "Jan", value: 19000 }, { month: "Feb", value: 24000 }, { month: "Mar", value: 21000 },
    ],
    recentPOs: [
      { id: "PO-2026-042", date: "Feb 18, 2026", amount: 24000, status: "received" },
      { id: "PO-2026-057", date: "Mar 05, 2026", amount: 28400, status: "pending"  },
    ],
  },
  {
    id: "V-004",
    name: "Al-Farhan Trading",
    contactName: "Omar Al-Farhan",
    email: "omar@alfarhan.sa",
    phone: "+966 12 345 6789",
    country: "Saudi Arabia",
    categories: ["Office Supplies", "Packaging"],
    paymentTerms: "Net 30",
    status: "active",
    totalOrders: 45,
    totalValue: 18900,
    outstanding: 2100,
    leadTimeDays: 3,
    rating: 4.9,
    since: "Jun 2021",
    linkedItems: [
      { sku: "OF-2001", name: "A4 Copy Paper — 80gsm",    inStock: 320, reorderLevel: 50,  unit: "ream", status: "overstocked" },
      { sku: "OF-2002", name: "Ballpoint Pen — Box of 50", inStock: 8,   reorderLevel: 20,  unit: "box",  status: "low-stock"   },
      { sku: "OF-2004", name: "File Folder — Manila A4",   inStock: 145, reorderLevel: 30,  unit: "pack", status: "overstocked" },
      { sku: "PK-5003", name: "Stretch Film — 500mm×400m", inStock: 0,   reorderLevel: 10,  unit: "roll", status: "out-of-stock"},
    ],
    monthlySpend: [
      { month: "Oct", value: 2800 }, { month: "Nov", value: 3100 }, { month: "Dec", value: 2600 },
      { month: "Jan", value: 3400 }, { month: "Feb", value: 2900 }, { month: "Mar", value: 3200 },
    ],
    recentPOs: [
      { id: "PO-2026-038", date: "Feb 12, 2026", amount: 2900, status: "received" },
      { id: "PO-2026-048", date: "Feb 28, 2026", amount: 2100, status: "received" },
      { id: "PO-2026-059", date: "Mar 11, 2026", amount: 3400, status: "pending"  },
    ],
  },
  {
    id: "V-005",
    name: "Summit Builders",
    contactName: "Lena Hoffmann",
    email: "lena@summitbuilders.de",
    phone: "+49 30 1234 5678",
    country: "Germany",
    categories: ["Office Supplies"],
    paymentTerms: "Net 30",
    status: "active",
    totalOrders: 12,
    totalValue: 9800,
    outstanding: 1400,
    leadTimeDays: 21,
    rating: 3.8,
    since: "Nov 2024",
    linkedItems: [
      { sku: "OF-2003", name: "Stapler — Heavy Duty",      inStock: 0,  reorderLevel: 5,  unit: "pcs", status: "out-of-stock" },
      { sku: "OF-2005", name: "Whiteboard Marker Set",     inStock: 42, reorderLevel: 15, unit: "set", status: "overstocked"  },
    ],
    monthlySpend: [
      { month: "Oct", value: 1200 }, { month: "Nov", value: 1500 }, { month: "Dec", value: 900 },
      { month: "Jan", value: 1800 }, { month: "Feb", value: 1600 }, { month: "Mar", value: 1400 },
    ],
    recentPOs: [
      { id: "PO-2026-044", date: "Feb 20, 2026", amount: 1600, status: "received" },
      { id: "PO-2026-056", date: "Mar 07, 2026", amount: 1400, status: "partial"  },
    ],
  },
  {
    id: "V-006",
    name: "Orion Healthcare",
    contactName: "Dr. Priya Sharma",
    email: "priya@orionhc.in",
    phone: "+91 22 5678 9012",
    country: "India",
    categories: ["Raw Materials"],
    paymentTerms: "Net 45",
    status: "inactive",
    totalOrders: 7,
    totalValue: 12400,
    outstanding: 0,
    leadTimeDays: 30,
    rating: 3.5,
    since: "Apr 2024",
    linkedItems: [
      { sku: "RM-3003", name: "Steel Rod — 10mm × 6m",    inStock: 0,  reorderLevel: 40, unit: "pcs", status: "out-of-stock" },
    ],
    monthlySpend: [
      { month: "Oct", value: 4000 }, { month: "Nov", value: 0 }, { month: "Dec", value: 3200 },
      { month: "Jan", value: 0 },    { month: "Feb", value: 2800 }, { month: "Mar", value: 0 },
    ],
    recentPOs: [
      { id: "PO-2025-089", date: "Feb 10, 2026", amount: 2800, status: "received" },
    ],
  },
  {
    id: "V-007",
    name: "Sunrise Retail Co.",
    contactName: "Chen Wei",
    email: "chenwei@sunrise.cn",
    phone: "+86 21 8765 4321",
    country: "China",
    categories: ["Packaging"],
    paymentTerms: "Net 30",
    status: "active",
    totalOrders: 28,
    totalValue: 22600,
    outstanding: 3800,
    leadTimeDays: 18,
    rating: 4.3,
    since: "Feb 2023",
    linkedItems: [
      { sku: "PK-5001", name: "Cardboard Box — 40×30×20cm", inStock: 850, reorderLevel: 200, unit: "pcs",  status: "overstocked" },
      { sku: "PK-5002", name: "Bubble Wrap Roll — 50m",     inStock: 12,  reorderLevel: 20,  unit: "roll", status: "low-stock"   },
      { sku: "PK-5004", name: "Packing Tape — 48mm×100m",   inStock: 230, reorderLevel: 50,  unit: "roll", status: "overstocked" },
    ],
    monthlySpend: [
      { month: "Oct", value: 3000 }, { month: "Nov", value: 4200 }, { month: "Dec", value: 3800 },
      { month: "Jan", value: 5100 }, { month: "Feb", value: 4600 }, { month: "Mar", value: 3900 },
    ],
    recentPOs: [
      { id: "PO-2026-039", date: "Feb 14, 2026", amount: 4600, status: "received" },
      { id: "PO-2026-052", date: "Mar 01, 2026", amount: 3800, status: "partial"  },
    ],
  },
];

// ─── Config ────────────────────────────────────────────────────────────────

const CAT_COLORS: Record<string, { color: string; bg: string }> = {
  "Electronics":     { color: "#3B82F6", bg: "#DBEAFE" },
  "Office Supplies": { color: "#8B5CF6", bg: "#DDD6FE" },
  "Raw Materials":   { color: "#F59E0B", bg: "#FDE68A" },
  "Packaging":       { color: "#64748B", bg: "#E2E8F0" },
};

const ITEM_STATUS_CFG = {
  "in-stock":     { color: "#065F46", bg: "#ECFDF5", dot: "#10B981" },
  "low-stock":    { color: "#92400E", bg: "#FFFBEB", dot: "#F59E0B" },
  "out-of-stock": { color: "#991B1B", bg: "#FEF2F2", dot: "#EF4444" },
  "overstocked":  { color: "#1D4ED8", bg: "#EFF6FF", dot: "#3B82F6" },
};

const PO_STATUS_CFG = {
  received: { label: "Received", color: "#065F46", bg: "#ECFDF5" },
  pending:  { label: "Pending",  color: "#1D4ED8", bg: "#EFF6FF" },
  partial:  { label: "Partial",  color: "#92400E", bg: "#FFFBEB" },
};

function StarRating({ rating }: { rating: number }) {
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 2 }}>
      {[1, 2, 3, 4, 5].map(i => (
        <Star key={i} size={11} style={{ color: i <= Math.round(rating) ? "#F59E0B" : "#E2E8F0", fill: i <= Math.round(rating) ? "#F59E0B" : "none" }} />
      ))}
      <span style={{ color: "#64748B", fontSize: 11, marginLeft: 3 }}>{rating.toFixed(1)}</span>
    </div>
  );
}

// ─── Custom Bar Chart ─────────────────────────────────────────────────────────

function CustomVendorBarChart({ data }: { data: { month: string; value: number }[] }) {
  const [hoverIdx, setHoverIdx] = useState<number | null>(null);
  const maxVal = Math.max(...data.map(d => d.value), 1000);
  
  const padT = 10;
  const padB = 20;
  const chartH = 90 - padT - padB;
  const W = 260; 
  const barW = 28;
  
  return (
    <div style={{ position: "relative", width: "100%", height: 90 }}>
      <svg viewBox={`0 0 ${W} 90`} width="100%" height="100%" style={{ display: "block", overflow: "visible" }}>
        {/* Horizontal grid lines */}
        {[0, 0.5, 1].map(ratio => {
          const y = padT + chartH * ratio;
          return <line key={`grid-${ratio}`} x1={0} y1={y} x2={W} y2={y} stroke="#F1F5F9" strokeWidth={1} strokeDasharray="3 3" />;
        })}
        
        {/* Bars */}
        {data.map((d, i) => {
          const spacing = (W - data.length * barW) / data.length;
          const x = spacing / 2 + i * (barW + spacing);
          const barH = (d.value / maxVal) * chartH;
          const y = padT + chartH - barH;
          const isHov = hoverIdx === i;
          
          return (
            <g key={`bar-${d.month}`} onMouseEnter={() => setHoverIdx(i)} onMouseLeave={() => setHoverIdx(null)} style={{ cursor: "pointer" }}>
              {/* Hover backdrop */}
              {isHov && <rect x={x - spacing/2} y={padT} width={barW + spacing} height={chartH} fill="#F0F7FF" rx={4} ry={4} />}
              {/* Bar */}
              {barH > 0 && (
                <>
                  <rect x={x} y={y} width={barW} height={barH} fill="#3B82F6" rx={4} ry={4} />
                  {barH > 4 && <rect x={x} y={y + barH - 4} width={barW} height={4} fill="#3B82F6" />}
                </>
              )}
              {/* X Axis label */}
              <text x={x + barW / 2} y={90 - 4} textAnchor="middle" fill="#94A3B8" fontSize={10} fontFamily="'Inter', sans-serif">
                {d.month}
              </text>
            </g>
          );
        })}
      </svg>
      {/* Tooltip */}
      {hoverIdx !== null && (
        <div style={{
          position: "absolute",
          left: `${((hoverIdx + 0.5) / data.length) * 100}%`,
          top: 0,
          transform: "translate(-50%, -5px)",
          backgroundColor: "#1E3A5F",
          borderRadius: 8,
          padding: "6px 10px",
          fontFamily: "'Inter', sans-serif",
          pointerEvents: "none",
          zIndex: 10,
          whiteSpace: "nowrap",
          textAlign: "center",
          boxShadow: "0 4px 12px rgba(0,0,0,0.15)"
        }}>
          <div style={{ color: "rgba(255,255,255,0.6)", fontSize: 10, marginBottom: 2 }}>{data[hoverIdx].month}</div>
          <div style={{ color: "#FFFFFF", fontSize: 12, fontWeight: 700 }}>${data[hoverIdx].value.toLocaleString()}</div>
        </div>
      )}
    </div>
  );
}

// ─── Vendor Detail Panel ────────────────────────────────────────────────────

function VendorDetail({ vendor }: { vendor: Vendor }) {
  const totalSpend = vendor.monthlySpend.reduce((s, m) => s + m.value, 0);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>

      {/* Header card */}
      <div style={{ backgroundColor: "#1E3A5F", borderRadius: 12, padding: 18, boxShadow: "0 4px 16px rgba(30,58,95,0.25)" }}>
        <div style={{ display: "flex", alignItems: "flex-start", justifyContent: "space-between", marginBottom: 12 }}>
          <div>
            <div style={{ color: "#FFFFFF", fontSize: 14, fontWeight: 800, marginBottom: 3 }}>{vendor.name}</div>
            <div style={{ display: "flex", gap: 5, flexWrap: "wrap" }}>
              {vendor.categories.map(c => {
                const cfg = CAT_COLORS[c] ?? { color: "#FFFFFF", bg: "rgba(255,255,255,0.2)" };
                return (
                  <span key={c} style={{ backgroundColor: "rgba(255,255,255,0.15)", color: "rgba(255,255,255,0.85)", fontSize: 9.5, fontWeight: 600, padding: "2px 7px", borderRadius: 8 }}>{c}</span>
                );
              })}
            </div>
          </div>
          <span style={{ backgroundColor: vendor.status === "active" ? "rgba(16,185,129,0.2)" : "rgba(239,68,68,0.2)", color: vendor.status === "active" ? "#10B981" : "#EF4444", fontSize: 10, fontWeight: 700, padding: "3px 9px", borderRadius: 12, border: `1px solid ${vendor.status === "active" ? "rgba(16,185,129,0.4)" : "rgba(239,68,68,0.4)"}` }}>
            {vendor.status === "active" ? "● Active" : "● Inactive"}
          </span>
        </div>
        <StarRating rating={vendor.rating} />
        <div style={{ marginTop: 10, display: "grid", gridTemplateColumns: "1fr 1fr", gap: 8 }}>
          {[
            { label: "Total Orders",   value: vendor.totalOrders.toString(), color: "#FFFFFF" },
            { label: "Lead Time",       value: `${vendor.leadTimeDays} days`,  color: "#FFFFFF" },
            { label: "Total Value",     value: `$${(vendor.totalValue/1000).toFixed(1)}k`, color: "#10B981" },
            { label: "Outstanding",     value: vendor.outstanding > 0 ? `$${vendor.outstanding.toLocaleString()}` : "—", color: vendor.outstanding > 0 ? "#F59E0B" : "#10B981" },
          ].map(m => (
            <div key={m.label} style={{ backgroundColor: "rgba(255,255,255,0.06)", borderRadius: 8, padding: "8px 10px" }}>
              <div style={{ color: "rgba(255,255,255,0.45)", fontSize: 9.5, marginBottom: 3 }}>{m.label}</div>
              <div style={{ color: m.color, fontSize: 14, fontWeight: 800 }}>{m.value}</div>
            </div>
          ))}
        </div>
      </div>

      {/* Contact */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px", marginBottom: 10 }}>Contact Information</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 7 }}>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Users size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
            <span style={{ color: "#0F172A", fontSize: 12.5, fontWeight: 600 }}>{vendor.contactName}</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Mail size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
            <span style={{ color: "#3B82F6", fontSize: 12 }}>{vendor.email}</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Phone size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
            <span style={{ color: "#334155", fontSize: 12 }}>{vendor.phone}</span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <Building2 size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
            <span style={{ color: "#334155", fontSize: 12 }}>{vendor.country} · {vendor.paymentTerms} · Since {vendor.since}</span>
          </div>
        </div>
      </div>

      {/* Linked Inventory Items */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10 }}>
          <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px" }}>Linked Items ({vendor.linkedItems.length})</div>
          <Package size={12} style={{ color: "#94A3B8" }} />
        </div>
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {vendor.linkedItems.map(item => {
            const sc = ITEM_STATUS_CFG[item.status];
            return (
              <div key={item.sku} style={{ display: "flex", alignItems: "center", gap: 8, padding: "7px 10px", borderRadius: 8, backgroundColor: "#F8FAFC", border: "1px solid #F1F5F9" }}>
                <div style={{ flex: 1, minWidth: 0 }}>
                  <div style={{ display: "flex", alignItems: "center", gap: 5 }}>
                    <span style={{ color: "#3B82F6", fontSize: 9.5, fontWeight: 700, fontFamily: "monospace" }}>{item.sku}</span>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 3, backgroundColor: sc.bg, color: sc.color, fontSize: 9, fontWeight: 600, padding: "1px 6px", borderRadius: 8 }}>
                      <span style={{ width: 4, height: 4, borderRadius: "50%", backgroundColor: sc.dot, display: "inline-block" }} />
                      {item.status.replace("-", " ").replace(/\b\w/g, c => c.toUpperCase())}
                    </span>
                  </div>
                  <div style={{ color: "#334155", fontSize: 11, fontWeight: 500, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap", marginTop: 2 }}>{item.name}</div>
                </div>
                <div style={{ textAlign: "right", flexShrink: 0 }}>
                  <div style={{ color: "#0F172A", fontSize: 12, fontWeight: 700 }}>{item.inStock}</div>
                  <div style={{ color: "#94A3B8", fontSize: 9.5 }}>{item.unit}</div>
                </div>
              </div>
            );
          })}
        </div>
      </div>

      {/* Purchase History Chart */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: 10 }}>
          <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px" }}>6-Month Spend</div>
          <span style={{ color: "#94A3B8", fontSize: 11 }}>${(totalSpend / 1000).toFixed(1)}k total</span>
        </div>
        <CustomVendorBarChart data={vendor.monthlySpend} />
      </div>

      {/* Recent POs */}
      <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: 14, boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
        <div style={{ color: "#64748B", fontSize: 10, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.6px", marginBottom: 10 }}>Recent Purchase Orders</div>
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {vendor.recentPOs.map(po => {
            const cfg = PO_STATUS_CFG[po.status];
            return (
              <div key={po.id} style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "7px 10px", borderRadius: 8, backgroundColor: "#F8FAFC", border: "1px solid #F1F5F9" }}>
                <div>
                  <div style={{ color: "#3B82F6", fontSize: 11, fontWeight: 700, fontFamily: "monospace" }}>{po.id}</div>
                  <div style={{ color: "#94A3B8", fontSize: 10, marginTop: 1 }}>{po.date}</div>
                </div>
                <div style={{ textAlign: "right" }}>
                  <div style={{ color: "#0F172A", fontSize: 12, fontWeight: 700 }}>${po.amount.toLocaleString()}</div>
                  <span style={{ backgroundColor: cfg.bg, color: cfg.color, fontSize: 9, fontWeight: 600, padding: "1px 6px", borderRadius: 8 }}>{cfg.label}</span>
                </div>
              </div>
            );
          })}
        </div>

        {/* Quick Actions */}
        <div style={{ display: "flex", gap: 7, marginTop: 10 }}>
          <button style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 5, padding: "8px 0", borderRadius: 8, border: "1.5px solid #1E3A5F", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 11, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <ShoppingCart size={12} /> New PO
          </button>
          <button style={{ flex: 1, display: "flex", alignItems: "center", justifyContent: "center", gap: 5, padding: "8px 0", borderRadius: 8, border: "1.5px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 11, fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <ExternalLink size={12} /> View Profile
          </button>
        </div>
      </div>

    </div>
  );
}

// ─── Main Component ─────────────────────────────────────────────────────────

export function VendorsTab() {
  const [selectedId, setSelectedId]       = useState<string>(VENDORS[0].id);
  const [search, setSearch]               = useState("");
  const [statusFilter, setStatusFilter]   = useState<"All" | "active" | "inactive">("All");

  const selected = VENDORS.find(v => v.id === selectedId) ?? VENDORS[0];

  const filtered = VENDORS.filter(v => {
    const q = search.toLowerCase();
    const matchSearch = !q || v.name.toLowerCase().includes(q) || v.contactName.toLowerCase().includes(q) || v.categories.some(c => c.toLowerCase().includes(q));
    const matchStatus = statusFilter === "All" || v.status === statusFilter;
    return matchSearch && matchStatus;
  });

  // ── KPIs ──
  const totalVendors  = VENDORS.length;
  const activeCount   = VENDORS.filter(v => v.status === "active").length;
  const totalPayable  = VENDORS.reduce((s, v) => s + v.outstanding, 0);
  const avgLeadTime   = Math.round(VENDORS.reduce((s, v) => s + v.leadTimeDays, 0) / VENDORS.length);

  const kpiCards = [
    { title: "Total Vendors",   value: `${totalVendors}`,                                          change: "+1 this quarter", positive: true,  color: "#3B82F6", bg: "#EFF6FF", icon: <Building2 size={19} /> },
    { title: "Active Vendors",  value: `${activeCount}`,                                           change: `${totalVendors - activeCount} inactive`, positive: true, color: "#10B981", bg: "#ECFDF5", icon: <CheckCircle size={19} /> },
    { title: "Total Payable",   value: `$${(totalPayable / 1000).toFixed(0)}k`,                    change: "5 open invoices", positive: false, color: "#F59E0B", bg: "#FFFBEB", icon: <AlertCircle size={19} /> },
    { title: "Avg Lead Time",   value: `${avgLeadTime} days`,                                      change: "-2d vs last qtr", positive: true,  color: "#8B5CF6", bg: "#F5F3FF", icon: <Clock size={19} /> },
  ];

  const handleSelect = (id: string) => setSelectedId(id);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: 16, fontFamily: "'Inter', sans-serif" }}>

      {/* ── Page header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0, color: "#0F172A", fontSize: 16, fontWeight: 800 }}>Vendor Management</h2>
          <p style={{ margin: "2px 0 0", color: "#94A3B8", fontSize: 12 }}>Supplier directory with inventory links, PO history & spend analytics</p>
        </div>
        <div style={{ display: "flex", gap: 8 }}>
          <button style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 14px", borderRadius: 9, border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: 12, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Download size={13} /> Export
          </button>
          <button style={{ display: "flex", alignItems: "center", gap: 6, padding: "8px 16px", borderRadius: 9, border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: 12, fontWeight: 700, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Plus size={14} /> Add Vendor
          </button>
        </div>
      </div>

      {/* ── KPI Cards ── */}
      <div style={{ display: "flex", gap: 14 }}>
        {kpiCards.map(card => (
          <div key={card.title} style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, padding: "15px 18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)", display: "flex", alignItems: "center", gap: 13, position: "relative", overflow: "hidden" }}>
            <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: 3, backgroundColor: card.color, borderRadius: "12px 12px 0 0" }} />
            <div style={{ width: 40, height: 40, borderRadius: 10, backgroundColor: card.bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <span style={{ color: card.color }}>{card.icon}</span>
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: "#64748B", fontSize: 11, fontWeight: 500, marginBottom: 3 }}>{card.title}</div>
              <div style={{ color: "#0F172A", fontSize: 20, fontWeight: 800, letterSpacing: "-0.4px", lineHeight: 1 }}>{card.value}</div>
            </div>
            <span style={{ display: "inline-flex", alignItems: "center", gap: 3, backgroundColor: card.positive ? "#ECFDF5" : "#FEF2F2", color: card.positive ? "#10B981" : "#EF4444", fontSize: 10, fontWeight: 700, padding: "3px 7px", borderRadius: 20, flexShrink: 0 }}>
              {card.positive ? <ArrowUpRight size={10} /> : <ArrowUpRight size={10} style={{ transform: "rotate(90deg)" }} />}
              {card.change}
            </span>
          </div>
        ))}
      </div>

      {/* ── Main: List + Detail ── */}
      <div style={{ display: "flex", gap: 16, alignItems: "flex-start" }}>

        {/* ── Vendor List ── */}
        <div style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: 12, boxShadow: "0 1px 4px rgba(0,0,0,0.05)", overflow: "hidden", minWidth: 0 }}>

          {/* Toolbar */}
          <div style={{ padding: "13px 18px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", gap: 10 }}>
            <div style={{ display: "flex", alignItems: "center", gap: 7, padding: "6px 12px", borderRadius: 8, border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", flex: 1 }}>
              <Search size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
              <input type="text" placeholder="Search vendors, contacts, categories…" value={search} onChange={e => setSearch(e.target.value)}
                style={{ flex: 1, border: "none", outline: "none", background: "transparent", color: "#334155", fontSize: 12, fontFamily: "'Inter', sans-serif" }} />
            </div>
            <div style={{ display: "flex", gap: 3, backgroundColor: "#F5F7FA", padding: 3, borderRadius: 8 }}>
              {(["All", "active", "inactive"] as const).map(f => (
                <button key={f} onClick={() => setStatusFilter(f)}
                  style={{ padding: "4px 10px", borderRadius: 6, border: "none", backgroundColor: statusFilter === f ? "#FFFFFF" : "transparent", color: statusFilter === f ? "#1E3A5F" : "#64748B", fontSize: 11.5, fontWeight: statusFilter === f ? 700 : 400, cursor: "pointer", boxShadow: statusFilter === f ? "0 1px 3px rgba(0,0,0,0.08)" : "none", fontFamily: "'Inter', sans-serif", textTransform: "capitalize" }}>
                  {f}
                </button>
              ))}
            </div>
          </div>

          {/* Table */}
          <div style={{ overflowY: "auto", maxHeight: 490 }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead style={{ position: "sticky", top: 0, zIndex: 2 }}>
                <tr style={{ backgroundColor: "#F8FAFC" }}>
                  {["Vendor", "Contact", "Categories", "Items", "Orders", "Total Value", "Outstanding", "Status", ""].map(h => (
                    <th key={h} style={{ padding: "8px 14px", textAlign: ["Total Value", "Outstanding"].includes(h) ? "right" : "left", color: "#64748B", fontSize: 10.5, fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.5px", borderBottom: "1px solid #E2E8F0", whiteSpace: "nowrap", backgroundColor: "#F8FAFC" }}>{h}</th>
                  ))}
                </tr>
              </thead>
              {filtered.map((vendor, idx) => {
                const isSelected = vendor.id === selectedId;
                return (
                  <tbody key={vendor.id}>
                    <tr
                      onClick={() => handleSelect(vendor.id)}
                      style={{ backgroundColor: isSelected ? "#F0F7FF" : idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC", borderBottom: "1px solid #F1F5F9", cursor: "pointer", borderLeft: isSelected ? "3px solid #3B82F6" : "3px solid transparent" }}
                      onMouseEnter={e => { if (!isSelected) (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F8FAFC"; }}
                      onMouseLeave={e => { if (!isSelected) (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC"; }}>
                      <td style={{ padding: "11px 14px" }}>
                        <div style={{ display: "flex", alignItems: "center", gap: 9 }}>
                          <div style={{ width: 32, height: 32, borderRadius: 8, background: "linear-gradient(135deg, #1E3A5F, #3B82F6)", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                            <span style={{ color: "#FFFFFF", fontSize: 11, fontWeight: 800 }}>{vendor.name.charAt(0)}</span>
                          </div>
                          <div>
                            <div style={{ color: "#0F172A", fontSize: 12.5, fontWeight: 700 }}>{vendor.name}</div>
                            <div style={{ color: "#94A3B8", fontSize: 10.5 }}>{vendor.id} · Since {vendor.since}</div>
                          </div>
                        </div>
                      </td>
                      <td style={{ padding: "11px 14px" }}>
                        <div style={{ color: "#334155", fontSize: 12 }}>{vendor.contactName}</div>
                        <div style={{ color: "#94A3B8", fontSize: 10.5 }}>{vendor.country}</div>
                      </td>
                      <td style={{ padding: "11px 14px" }}>
                        <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                          {vendor.categories.map(c => {
                            const cfg = CAT_COLORS[c] ?? { color: "#64748B", bg: "#E2E8F0" };
                            return (
                              <span key={c} style={{ backgroundColor: cfg.bg, color: cfg.color, fontSize: 9.5, fontWeight: 600, padding: "2px 6px", borderRadius: 6 }}>{c}</span>
                            );
                          })}
                        </div>
                      </td>
                      <td style={{ padding: "11px 14px", color: "#334155", fontSize: 12, fontWeight: 600 }}>
                        {vendor.linkedItems.length}
                      </td>
                      <td style={{ padding: "11px 14px", color: "#334155", fontSize: 12 }}>{vendor.totalOrders}</td>
                      <td style={{ padding: "11px 14px", textAlign: "right", color: "#0F172A", fontSize: 13, fontWeight: 700 }}>
                        ${vendor.totalValue.toLocaleString()}
                      </td>
                      <td style={{ padding: "11px 14px", textAlign: "right" }}>
                        <span style={{ color: vendor.outstanding > 0 ? "#F59E0B" : "#10B981", fontSize: 12.5, fontWeight: 700 }}>
                          {vendor.outstanding > 0 ? `$${vendor.outstanding.toLocaleString()}` : "—"}
                        </span>
                      </td>
                      <td style={{ padding: "11px 14px" }}>
                        <span style={{ display: "inline-flex", alignItems: "center", gap: 4, backgroundColor: vendor.status === "active" ? "#ECFDF5" : "#F1F5F9", color: vendor.status === "active" ? "#065F46" : "#64748B", fontSize: 10.5, fontWeight: 600, padding: "2px 8px", borderRadius: 20 }}>
                          <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: vendor.status === "active" ? "#10B981" : "#94A3B8", display: "inline-block" }} />
                          {vendor.status === "active" ? "Active" : "Inactive"}
                        </span>
                      </td>
                      <td style={{ padding: "11px 14px" }}>
                        <ChevronRight size={14} style={{ color: isSelected ? "#3B82F6" : "#CBD5E1" }} />
                      </td>
                    </tr>
                  </tbody>
                );
              })}
            </table>
          </div>

          <div style={{ padding: "10px 18px", borderTop: "1px solid #F1F5F9" }}>
            <span style={{ color: "#94A3B8", fontSize: 12 }}>Showing {filtered.length} of {VENDORS.length} vendors</span>
          </div>
        </div>

        {/* ── Detail Panel ── */}
        <div style={{ width: 300, minWidth: 300, maxHeight: 570, overflowY: "auto" }}>
          <VendorDetail vendor={selected} />
        </div>

      </div>
    </div>
  );
}