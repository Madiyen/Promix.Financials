import React, { useState, useMemo } from "react";
import { PieChart, Pie, Tooltip } from "recharts";
import {
  Search, Plus, Download, Filter, ChevronDown, ChevronRight,
  Eye, Pencil, MoreHorizontal, ArrowUpRight, ArrowDownRight,
  Package, AlertTriangle, XCircle, TrendingUp, RefreshCw,
  Truck, BarChart2, Tag, SlidersHorizontal,
} from "lucide-react";
import { AdjustStockModal, AuditEntry } from "./AdjustStockModal";

// ─── Types ─────────────────────────────────────────────────────────────────

type StockStatus   = "in-stock" | "low-stock" | "out-of-stock" | "overstocked";
type InventoryCategory = "Electronics" | "Office Supplies" | "Raw Materials" | "Finished Goods" | "Packaging";

interface InventoryItem {
  sku: string;
  name: string;
  category: InventoryCategory;
  unit: string;
  inStock: number;
  reorderLevel: number;
  unitCost: number;
  supplier: string;
  lastUpdated: string;
}

// ─── Raw Data ──────────────────────────────────────────────────────────────

const rawItems: InventoryItem[] = [
  // Electronics
  { sku: "EL-1001", name: "Laptop — Dell Inspiron 15",     category: "Electronics",     unit: "pcs",  inStock: 24,  reorderLevel: 10, unitCost: 849.00,  supplier: "TechCorp Solutions",   lastUpdated: "Feb 26, 2026" },
  { sku: "EL-1002", name: "Wireless Mouse — Logitech MX",  category: "Electronics",     unit: "pcs",  inStock: 5,   reorderLevel: 20, unitCost: 59.99,   supplier: "TechCorp Solutions",   lastUpdated: "Feb 24, 2026" },
  { sku: "EL-1003", name: "USB-C Hub — 7-in-1",            category: "Electronics",     unit: "pcs",  inStock: 0,   reorderLevel: 15, unitCost: 34.50,   supplier: "NextGen Supplies",     lastUpdated: "Feb 20, 2026" },
  { sku: "EL-1004", name: "Monitor — 27\" 4K IPS",         category: "Electronics",     unit: "pcs",  inStock: 88,  reorderLevel: 8,  unitCost: 520.00,  supplier: "Global Imports LLC",   lastUpdated: "Feb 25, 2026" },
  { sku: "EL-1005", name: "Mechanical Keyboard",           category: "Electronics",     unit: "pcs",  inStock: 3,   reorderLevel: 10, unitCost: 115.00,  supplier: "NextGen Supplies",     lastUpdated: "Feb 22, 2026" },
  { sku: "EL-1006", name: "Webcam — HD 1080p",             category: "Electronics",     unit: "pcs",  inStock: 17,  reorderLevel: 12, unitCost: 89.00,   supplier: "TechCorp Solutions",   lastUpdated: "Feb 23, 2026" },
  // Office Supplies
  { sku: "OF-2001", name: "A4 Copy Paper — 80gsm (Ream)",  category: "Office Supplies", unit: "ream", inStock: 320, reorderLevel: 50, unitCost: 4.75,    supplier: "Al-Farhan Trading",    lastUpdated: "Feb 26, 2026" },
  { sku: "OF-2002", name: "Ballpoint Pen — Box of 50",     category: "Office Supplies", unit: "box",  inStock: 8,   reorderLevel: 20, unitCost: 12.00,   supplier: "Al-Farhan Trading",    lastUpdated: "Feb 18, 2026" },
  { sku: "OF-2003", name: "Stapler — Heavy Duty",          category: "Office Supplies", unit: "pcs",  inStock: 0,   reorderLevel: 5,  unitCost: 18.50,   supplier: "Summit Builders",      lastUpdated: "Feb 10, 2026" },
  { sku: "OF-2004", name: "File Folder — Manila A4",       category: "Office Supplies", unit: "pack", inStock: 145, reorderLevel: 30, unitCost: 3.20,    supplier: "Al-Farhan Trading",    lastUpdated: "Feb 21, 2026" },
  { sku: "OF-2005", name: "Whiteboard Marker Set",         category: "Office Supplies", unit: "set",  inStock: 42,  reorderLevel: 15, unitCost: 8.90,    supplier: "Summit Builders",      lastUpdated: "Feb 19, 2026" },
  // Raw Materials
  { sku: "RM-3001", name: "Aluminium Sheet — 2mm",         category: "Raw Materials",   unit: "kg",   inStock: 640, reorderLevel: 200, unitCost: 3.80,   supplier: "Global Imports LLC",   lastUpdated: "Feb 25, 2026" },
  { sku: "RM-3002", name: "PVC Granules — Grade A",        category: "Raw Materials",   unit: "kg",   inStock: 95,  reorderLevel: 150, unitCost: 1.45,   supplier: "Global Imports LLC",   lastUpdated: "Feb 24, 2026" },
  { sku: "RM-3003", name: "Steel Rod — 10mm × 6m",         category: "Raw Materials",   unit: "pcs",  inStock: 0,   reorderLevel: 40,  unitCost: 22.00,  supplier: "Orion Healthcare",     lastUpdated: "Feb 15, 2026" },
  { sku: "RM-3004", name: "Industrial Adhesive — 5L",      category: "Raw Materials",   unit: "can",  inStock: 28,  reorderLevel: 20,  unitCost: 47.00,  supplier: "NextGen Supplies",     lastUpdated: "Feb 22, 2026" },
  { sku: "RM-3005", name: "Copper Wire — 2.5mm²",          category: "Raw Materials",   unit: "roll", inStock: 180, reorderLevel: 50,  unitCost: 28.00,  supplier: "Global Imports LLC",   lastUpdated: "Feb 26, 2026" },
  // Finished Goods
  { sku: "FG-4001", name: "Control Panel Unit — Model A",  category: "Finished Goods",  unit: "pcs",  inStock: 14,  reorderLevel: 10, unitCost: 380.00,  supplier: "Internal",             lastUpdated: "Feb 24, 2026" },
  { sku: "FG-4002", name: "Power Distribution Box",        category: "Finished Goods",  unit: "pcs",  inStock: 6,   reorderLevel: 8,  unitCost: 220.00,  supplier: "Internal",             lastUpdated: "Feb 23, 2026" },
  { sku: "FG-4003", name: "Solar Inverter — 5kW",          category: "Finished Goods",  unit: "pcs",  inStock: 0,   reorderLevel: 5,  unitCost: 950.00,  supplier: "Internal",             lastUpdated: "Feb 20, 2026" },
  { sku: "FG-4004", name: "LED Driver Module — 60W",       category: "Finished Goods",  unit: "pcs",  inStock: 52,  reorderLevel: 20, unitCost: 45.00,   supplier: "Internal",             lastUpdated: "Feb 26, 2026" },
  { sku: "FG-4005", name: "Smart Meter — Single Phase",    category: "Finished Goods",  unit: "pcs",  inStock: 9,   reorderLevel: 15, unitCost: 140.00,  supplier: "Internal",             lastUpdated: "Feb 25, 2026" },
  // Packaging
  { sku: "PK-5001", name: "Cardboard Box — 40×30×20cm",   category: "Packaging",       unit: "pcs",  inStock: 850, reorderLevel: 200, unitCost: 0.95,   supplier: "Sunrise Retail Co.",   lastUpdated: "Feb 26, 2026" },
  { sku: "PK-5002", name: "Bubble Wrap Roll — 50m",        category: "Packaging",       unit: "roll", inStock: 12,  reorderLevel: 20,  unitCost: 18.00,  supplier: "Sunrise Retail Co.",   lastUpdated: "Feb 21, 2026" },
  { sku: "PK-5003", name: "Stretch Film — 500mm×400m",     category: "Packaging",       unit: "roll", inStock: 0,   reorderLevel: 10,  unitCost: 14.50,  supplier: "Al-Farhan Trading",    lastUpdated: "Feb 17, 2026" },
  { sku: "PK-5004", name: "Packing Tape — 48mm×100m",      category: "Packaging",       unit: "roll", inStock: 230, reorderLevel: 50,  unitCost: 2.80,   supplier: "Sunrise Retail Co.",   lastUpdated: "Feb 25, 2026" },
];

// ─── Seed Audit Log ────────────────────────────────────────────────────────

const SEED_AUDIT: AuditEntry[] = [
  { id: "seed-1", sku: "EL-1001", date: "Feb 22, 2026", time: "09:15 AM", user: "Ahmed Hassan",  adjType: "Add",    before: 20,  after: 24,  change: 4,   reason: "Purchase Receipt",     refNum: "PO-2026-045", notes: "",                             unit: "pcs"  },
  { id: "seed-2", sku: "EL-1002", date: "Feb 20, 2026", time: "11:30 AM", user: "Sara Ahmed",    adjType: "Remove", before: 12,  after: 5,   change: -7,  reason: "Sales Return",         refNum: "SO-2026-112", notes: "Defective units returned",     unit: "pcs"  },
  { id: "seed-3", sku: "OF-2001", date: "Feb 18, 2026", time: "02:45 PM", user: "Ahmed Hassan",  adjType: "Add",    before: 280, after: 320, change: 40,  reason: "Purchase Receipt",     refNum: "PO-2026-038", notes: "",                             unit: "ream" },
  { id: "seed-4", sku: "RM-3001", date: "Feb 15, 2026", time: "10:00 AM", user: "Khalid Nasser", adjType: "Set To", before: 700, after: 640, change: -60, reason: "Stock Count Correction",refNum: "",            notes: "Annual count reconciliation",  unit: "kg"   },
  { id: "seed-5", sku: "EL-1004", date: "Feb 14, 2026", time: "03:20 PM", user: "Sara Ahmed",    adjType: "Add",    before: 72,  after: 88,  change: 16,  reason: "Purchase Receipt",     refNum: "PO-2026-031", notes: "",                             unit: "pcs"  },
  { id: "seed-6", sku: "RM-3005", date: "Feb 12, 2026", time: "01:10 PM", user: "Ahmed Hassan",  adjType: "Add",    before: 150, after: 180, change: 30,  reason: "Transfer In",          refNum: "TR-2026-009", notes: "From Dubai warehouse",         unit: "roll" },
];

// ─── Helpers ───────────────────────────────────────────────────────────────

function deriveStatus(item: { inStock: number; reorderLevel: number }): StockStatus {
  if (item.inStock === 0) return "out-of-stock";
  if (item.inStock <= item.reorderLevel) return "low-stock";
  if (item.inStock >= item.reorderLevel * 4) return "overstocked";
  return "in-stock";
}

function buildItem(raw: InventoryItem) {
  return { ...raw, status: deriveStatus(raw), totalValue: raw.inStock * raw.unitCost };
}

type DerivedItem = ReturnType<typeof buildItem>;

// ─── Config ─────────────────────────────────────────────────────────────────

const categoryConfig: Record<InventoryCategory, { color: string; bg: string; light: string; icon: React.ReactNode }> = {
  "Electronics":     { color: "#3B82F6", bg: "#EFF6FF", light: "#DBEAFE", icon: <BarChart2 size={15} /> },
  "Office Supplies": { color: "#8B5CF6", bg: "#F5F3FF", light: "#DDD6FE", icon: <Tag size={15} /> },
  "Raw Materials":   { color: "#F59E0B", bg: "#FFFBEB", light: "#FDE68A", icon: <Package size={15} /> },
  "Finished Goods":  { color: "#10B981", bg: "#ECFDF5", light: "#A7F3D0", icon: <TrendingUp size={15} /> },
  "Packaging":       { color: "#64748B", bg: "#F8FAFC", light: "#E2E8F0", icon: <Truck size={15} /> },
};

const statusConfig: Record<StockStatus, { label: string; color: string; bg: string; dot: string }> = {
  "in-stock":     { label: "In Stock",     color: "#065F46", bg: "#ECFDF5", dot: "#10B981" },
  "low-stock":    { label: "Low Stock",    color: "#92400E", bg: "#FFFBEB", dot: "#F59E0B" },
  "out-of-stock": { label: "Out of Stock", color: "#991B1B", bg: "#FEF2F2", dot: "#EF4444" },
  "overstocked":  { label: "Overstocked",  color: "#1D4ED8", bg: "#EFF6FF", dot: "#3B82F6" },
};

const categoryOrder: InventoryCategory[] = ["Electronics", "Office Supplies", "Raw Materials", "Finished Goods", "Packaging"];

const filterTabs = [
  { key: "All",             label: "All Items" },
  { key: "Electronics",     label: "Electronics" },
  { key: "Office Supplies", label: "Office Supplies" },
  { key: "Raw Materials",   label: "Raw Materials" },
  { key: "Finished Goods",  label: "Finished Goods" },
  { key: "Packaging",       label: "Packaging" },
];

// ─── Sub-components ──────────────────────────────────────────────────────────

const StatusBadge = ({ status }: { status: StockStatus }) => {
  const cfg = statusConfig[status];
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: "4px", backgroundColor: cfg.bg, color: cfg.color, fontSize: "10.5px", fontWeight: 600, padding: "2px 8px", borderRadius: "20px", whiteSpace: "nowrap" }}>
      <span style={{ width: 5, height: 5, borderRadius: "50%", backgroundColor: cfg.dot, display: "inline-block", flexShrink: 0 }} />
      {cfg.label}
    </span>
  );
};

const CustomPieTooltip = ({ active, payload }: any) => {
  if (active && payload && payload.length) {
    return (
      <div style={{ backgroundColor: "#1E3A5F", borderRadius: "8px", padding: "7px 11px", fontFamily: "'Inter', sans-serif" }}>
        <span style={{ color: "#FFFFFF", fontSize: "12px", fontWeight: 600 }}>
          {payload[0].name}: {payload[0].value} items
        </span>
      </div>
    );
  }
  return null;
};

function StockBar({ inStock, reorderLevel }: { inStock: number; reorderLevel: number }) {
  if (inStock === 0) {
    return <div style={{ height: 5, borderRadius: 3, backgroundColor: "#FEE2E2", width: "80px" }}><div style={{ height: "100%", width: "0%", backgroundColor: "#EF4444", borderRadius: 3 }} /></div>;
  }
  const max = Math.max(inStock, reorderLevel * 2, 1);
  const pct = Math.min((inStock / max) * 100, 100);
  const color = inStock <= reorderLevel ? "#F59E0B" : inStock >= reorderLevel * 4 ? "#3B82F6" : "#10B981";
  return (
    <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
      <div style={{ height: 5, borderRadius: 3, backgroundColor: "#F1F5F9", width: "60px", overflow: "hidden" }}>
        <div style={{ height: "100%", width: `${pct}%`, backgroundColor: color, borderRadius: 3 }} />
      </div>
      <span style={{ color: "#64748B", fontSize: "11px", fontVariantNumeric: "tabular-nums", minWidth: "28px" }}>{inStock.toLocaleString()}</span>
    </div>
  );
}

function CategoryHeader({ category, count, totalValue, expanded, onToggle }: {
  category: InventoryCategory; count: number; totalValue: number; expanded: boolean; onToggle: () => void;
}) {
  const cfg = categoryConfig[category];
  return (
    <tr onClick={onToggle} style={{ cursor: "pointer", backgroundColor: cfg.bg }}>
      <td colSpan={10} style={{ padding: "8px 16px" }}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div style={{ display: "flex", alignItems: "center", gap: "10px" }}>
            <div style={{ width: 26, height: 26, borderRadius: 7, backgroundColor: cfg.light, display: "flex", alignItems: "center", justifyContent: "center" }}>
              <span style={{ color: cfg.color }}>{cfg.icon}</span>
            </div>
            <span style={{ color: cfg.color, fontSize: "12.5px", fontWeight: 700 }}>{category}</span>
            <span style={{ backgroundColor: cfg.light, color: cfg.color, fontSize: "10px", fontWeight: 700, padding: "1px 7px", borderRadius: "10px" }}>
              {count} {count === 1 ? "item" : "items"}
            </span>
          </div>
          <div style={{ display: "flex", alignItems: "center", gap: "16px" }}>
            <span style={{ color: cfg.color, fontSize: "12.5px", fontWeight: 800 }}>
              ${totalValue.toLocaleString("en-US", { minimumFractionDigits: 2 })}
            </span>
            {expanded ? <ChevronDown size={14} style={{ color: cfg.color }} /> : <ChevronRight size={14} style={{ color: cfg.color }} />}
          </div>
        </div>
      </td>
    </tr>
  );
}

// ─── Main Component ──────────────────────────────────────────────────────────

export function InventoryTab() {
  const [inventoryItems, setInventoryItems] = useState<DerivedItem[]>(() => rawItems.map(buildItem));
  const [auditLog, setAuditLog]             = useState<AuditEntry[]>(SEED_AUDIT);
  const [adjustingItem, setAdjustingItem]   = useState<DerivedItem | null>(null);
  const [activeFilter, setActiveFilter]     = useState("All");
  const [searchQuery, setSearchQuery]       = useState("");
  const [expandedGroups, setExpandedGroups] = useState<Set<string>>(new Set(categoryOrder));

  const toggleGroup = (cat: string) => {
    setExpandedGroups(prev => {
      const next = new Set(prev);
      next.has(cat) ? next.delete(cat) : next.add(cat);
      return next;
    });
  };

  const handleAdjust = (sku: string, newQty: number, entry: AuditEntry) => {
    setInventoryItems(prev => prev.map(it => {
      if (it.sku !== sku) return it;
      const updated = { ...it, inStock: newQty, lastUpdated: entry.date };
      return { ...updated, status: deriveStatus(updated), totalValue: updated.inStock * updated.unitCost };
    }));
    setAuditLog(prev => [entry, ...prev]);
    setAdjustingItem(null);
  };

  // ── Filtered ──
  const filtered = useMemo(() => {
    return inventoryItems.filter(item => {
      const matchCat    = activeFilter === "All" || item.category === activeFilter;
      const q           = searchQuery.toLowerCase();
      const matchSearch = !q || item.name.toLowerCase().includes(q) || item.sku.toLowerCase().includes(q) || item.supplier.toLowerCase().includes(q);
      return matchCat && matchSearch;
    });
  }, [inventoryItems, activeFilter, searchQuery]);

  // ── KPIs ──
  const totalSKUs       = inventoryItems.length;
  const totalValue      = inventoryItems.reduce((s, i) => s + i.totalValue, 0);
  const lowStockCount   = inventoryItems.filter(i => i.status === "low-stock").length;
  const outOfStockCount = inventoryItems.filter(i => i.status === "out-of-stock").length;

  const kpiCards = [
    { title: "Total SKUs",        value: `${totalSKUs}`,                               change: "+3",                         positive: true,  color: "#3B82F6", bg: "#EFF6FF", icon: <Package size={20} /> },
    { title: "Total Stock Value", value: `$${(totalValue / 1000).toFixed(1)}k`,        change: "+11.2%",                     positive: true,  color: "#10B981", bg: "#ECFDF5", icon: <TrendingUp size={20} /> },
    { title: "Low Stock Items",   value: `${lowStockCount}`,                            change: `+${Math.max(0, lowStockCount - 4)}`,   positive: false, color: "#F59E0B", bg: "#FFFBEB", icon: <AlertTriangle size={20} /> },
    { title: "Out of Stock",      value: `${outOfStockCount}`,                          change: `+${Math.max(0, outOfStockCount - 2)}`, positive: false, color: "#EF4444", bg: "#FEF2F2", icon: <XCircle size={20} /> },
  ];

  // ── Donut ──
  const statusBreakdown = [
    { name: "In Stock",     value: inventoryItems.filter(i => i.status === "in-stock").length,     color: "#10B981" },
    { name: "Low Stock",    value: inventoryItems.filter(i => i.status === "low-stock").length,    color: "#F59E0B" },
    { name: "Out of Stock", value: inventoryItems.filter(i => i.status === "out-of-stock").length, color: "#EF4444" },
    { name: "Overstocked",  value: inventoryItems.filter(i => i.status === "overstocked").length,  color: "#3B82F6" },
  ];

  // ── Top 5 by value ──
  const top5 = [...inventoryItems].sort((a, b) => b.totalValue - a.totalValue).slice(0, 5);
  const maxTopValue = top5[0]?.totalValue ?? 1;

  // ── Reorder alerts ──
  const reorderAlerts = inventoryItems.filter(i => i.status === "low-stock" || i.status === "out-of-stock")
    .sort((a, b) => a.inStock - b.inStock).slice(0, 5);

  const visibleCats = activeFilter === "All" ? categoryOrder : categoryOrder.filter(c => c === activeFilter);

  return (
    <div style={{ display: "flex", flexDirection: "column", gap: "16px", fontFamily: "'Inter', sans-serif" }}>

      {/* ── Page title ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h2 style={{ margin: 0, color: "#0F172A", fontSize: "16px", fontWeight: 800 }}>Inventory Management</h2>
          <p style={{ margin: "2px 0 0", color: "#94A3B8", fontSize: "12px" }}>
            Track stock levels, valuations & reorder alerts · FY 2025–26
          </p>
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
          <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "8px 14px", borderRadius: "9px", border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: "12px", cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <RefreshCw size={13} /> Refresh
          </button>
          <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "8px 14px", borderRadius: "9px", border: "1px solid #E2E8F0", backgroundColor: "#FFFFFF", color: "#64748B", fontSize: "12px", cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Download size={13} /> Export
          </button>
          <button style={{ display: "flex", alignItems: "center", gap: "6px", padding: "8px 16px", borderRadius: "9px", border: "none", backgroundColor: "#1E3A5F", color: "#FFFFFF", fontSize: "12px", fontWeight: 600, cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
            <Plus size={14} /> Add Item
          </button>
        </div>
      </div>

      {/* ── KPI Cards ── */}
      <div style={{ display: "flex", gap: "14px" }}>
        {kpiCards.map(card => (
          <div key={card.title} style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: "12px", padding: "16px 18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)", display: "flex", alignItems: "center", gap: "14px", position: "relative", overflow: "hidden" }}>
            <div style={{ position: "absolute", top: 0, left: 0, right: 0, height: "3px", backgroundColor: card.color, borderRadius: "12px 12px 0 0" }} />
            <div style={{ width: 42, height: 42, borderRadius: 10, backgroundColor: card.bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
              <span style={{ color: card.color }}>{card.icon}</span>
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ color: "#64748B", fontSize: "11px", fontWeight: 500, marginBottom: "3px" }}>{card.title}</div>
              <div style={{ color: "#0F172A", fontSize: "20px", fontWeight: 800, letterSpacing: "-0.4px", lineHeight: 1 }}>{card.value}</div>
            </div>
            <span style={{ display: "inline-flex", alignItems: "center", gap: "3px", backgroundColor: card.positive ? "#ECFDF5" : "#FEF2F2", color: card.positive ? "#10B981" : "#EF4444", fontSize: "10.5px", fontWeight: 700, padding: "3px 8px", borderRadius: "20px", flexShrink: 0 }}>
              {card.positive ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
              {card.change}
            </span>
          </div>
        ))}
      </div>

      {/* ── Main: Table + Right Panel ── */}
      <div style={{ display: "flex", gap: "16px", alignItems: "flex-start" }}>

        {/* ── Table Panel ── */}
        <div style={{ flex: 1, backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: "12px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)", overflow: "hidden", minWidth: 0 }}>

          {/* Filter bar */}
          <div style={{ padding: "13px 18px", borderBottom: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between", gap: "12px" }}>
            <div style={{ display: "flex", gap: "3px", backgroundColor: "#F5F7FA", padding: "3px", borderRadius: "10px", flexShrink: 0 }}>
              {filterTabs.map(tab => {
                const isActive = activeFilter === tab.key;
                const cfg = tab.key !== "All" ? categoryConfig[tab.key as InventoryCategory] : null;
                const count = tab.key === "All" ? inventoryItems.length : inventoryItems.filter(i => i.category === tab.key).length;
                return (
                  <button key={tab.key} onClick={() => setActiveFilter(tab.key)}
                    style={{ padding: "4px 10px", borderRadius: "7px", border: "none", backgroundColor: isActive ? "#FFFFFF" : "transparent", color: isActive ? (cfg?.color ?? "#1E3A5F") : "#64748B", fontSize: "11.5px", fontWeight: isActive ? 700 : 400, cursor: "pointer", boxShadow: isActive ? "0 1px 3px rgba(0,0,0,0.1)" : "none", display: "flex", alignItems: "center", gap: "4px", fontFamily: "'Inter', sans-serif", whiteSpace: "nowrap" }}>
                    {tab.label}
                    <span style={{ backgroundColor: isActive ? (cfg?.bg ?? "#EFF6FF") : "#E2E8F0", color: isActive ? (cfg?.color ?? "#1E3A5F") : "#94A3B8", fontSize: "9.5px", fontWeight: 700, padding: "1px 5px", borderRadius: "8px" }}>
                      {count}
                    </span>
                  </button>
                );
              })}
            </div>
            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
              <div style={{ display: "flex", alignItems: "center", gap: "8px", padding: "6px 12px", borderRadius: "8px", border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", width: "200px" }}>
                <Search size={13} style={{ color: "#94A3B8", flexShrink: 0 }} />
                <input type="text" placeholder="Search SKU or name..." value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
                  style={{ flex: 1, border: "none", outline: "none", background: "transparent", color: "#334155", fontSize: "12px", fontFamily: "'Inter', sans-serif" }} />
              </div>
              <button style={{ display: "flex", alignItems: "center", gap: "5px", padding: "6px 11px", borderRadius: "8px", border: "1px solid #E2E8F0", backgroundColor: "#F8FAFC", color: "#64748B", fontSize: "11.5px", cursor: "pointer", fontFamily: "'Inter', sans-serif" }}>
                <Filter size={12} /> Status <ChevronDown size={11} />
              </button>
            </div>
          </div>

          {/* Table */}
          <div style={{ overflowX: "auto", maxHeight: "400px", overflowY: "auto" }}>
            <table style={{ width: "100%", borderCollapse: "collapse" }}>
              <thead style={{ position: "sticky", top: 0, zIndex: 2 }}>
                <tr style={{ backgroundColor: "#F8FAFC" }}>
                  {["SKU", "Product Name", "Unit", "In Stock", "Reorder Lvl", "Unit Cost", "Total Value", "Status", "Actions", ""].map(h => (
                    <th key={h} style={{ padding: "9px 14px", textAlign: ["Unit Cost", "Total Value"].includes(h) ? "right" : "left", color: "#64748B", fontSize: "10.5px", fontWeight: 700, textTransform: "uppercase", letterSpacing: "0.5px", borderBottom: "1px solid #E2E8F0", whiteSpace: "nowrap", backgroundColor: "#F8FAFC" }}>
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>

              {visibleCats.map(cat => {
                const catItems = filtered.filter(i => i.category === cat);
                if (catItems.length === 0) return null;
                const catValue = catItems.reduce((s, i) => s + i.totalValue, 0);
                const isExpanded = expandedGroups.has(cat);
                return (
                  <tbody key={cat}>
                    <CategoryHeader category={cat} count={catItems.length} totalValue={catValue} expanded={isExpanded} onToggle={() => toggleGroup(cat)} />
                    {isExpanded && catItems.map((item, idx) => {
                      const catCfg = categoryConfig[item.category];
                      return (
                        <tr key={item.sku}
                          style={{ backgroundColor: idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC", borderBottom: "1px solid #F1F5F9", transition: "background-color 0.1s" }}
                          onMouseEnter={e => { (e.currentTarget as HTMLTableRowElement).style.backgroundColor = "#F0F7FF"; }}
                          onMouseLeave={e => { (e.currentTarget as HTMLTableRowElement).style.backgroundColor = idx % 2 === 0 ? "#FFFFFF" : "#FAFBFC"; }}>
                          {/* SKU */}
                          <td style={{ padding: "10px 14px" }}>
                            <span style={{ color: catCfg.color, fontSize: "11.5px", fontWeight: 700, fontFamily: "monospace" }}>{item.sku}</span>
                          </td>
                          {/* Name */}
                          <td style={{ padding: "10px 14px" }}>
                            <div style={{ display: "flex", alignItems: "center", gap: "8px" }}>
                              <div style={{ width: 28, height: 28, borderRadius: 7, backgroundColor: catCfg.bg, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                                <span style={{ color: catCfg.color }}>{catCfg.icon}</span>
                              </div>
                              <div>
                                <div style={{ color: "#0F172A", fontSize: "12.5px", fontWeight: 600, whiteSpace: "nowrap" }}>{item.name}</div>
                                <div style={{ color: "#94A3B8", fontSize: "10.5px" }}>{item.supplier}</div>
                              </div>
                            </div>
                          </td>
                          {/* Unit */}
                          <td style={{ padding: "10px 14px" }}>
                            <span style={{ color: "#64748B", fontSize: "11.5px", backgroundColor: "#F1F5F9", padding: "2px 7px", borderRadius: "4px" }}>{item.unit}</span>
                          </td>
                          {/* In Stock */}
                          <td style={{ padding: "10px 14px" }}>
                            <StockBar inStock={item.inStock} reorderLevel={item.reorderLevel} />
                          </td>
                          {/* Reorder Level */}
                          <td style={{ padding: "10px 14px" }}>
                            <span style={{ color: "#94A3B8", fontSize: "12px", fontVariantNumeric: "tabular-nums" }}>{item.reorderLevel.toLocaleString()}</span>
                          </td>
                          {/* Unit Cost */}
                          <td style={{ padding: "10px 14px", textAlign: "right" }}>
                            <span style={{ color: "#334155", fontSize: "12.5px", fontWeight: 600, fontVariantNumeric: "tabular-nums" }}>
                              ${item.unitCost.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                            </span>
                          </td>
                          {/* Total Value */}
                          <td style={{ padding: "10px 14px", textAlign: "right" }}>
                            <span style={{ color: "#0F172A", fontSize: "13px", fontWeight: 700, fontVariantNumeric: "tabular-nums" }}>
                              ${item.totalValue.toLocaleString("en-US", { minimumFractionDigits: 2 })}
                            </span>
                          </td>
                          {/* Status */}
                          <td style={{ padding: "10px 14px" }}>
                            <StatusBadge status={item.status} />
                          </td>
                          {/* Actions */}
                          <td style={{ padding: "10px 14px" }}>
                            <div style={{ display: "flex", alignItems: "center", gap: "4px" }}>
                              <button title="View" style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#EFF6FF", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <Eye size={12} style={{ color: "#3B82F6" }} />
                              </button>
                              <button title="Edit" style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#F5F3FF", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <Pencil size={12} style={{ color: "#8B5CF6" }} />
                              </button>
                              <button title="Adjust Stock" onClick={() => setAdjustingItem(item)}
                                style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#ECFDF5", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <SlidersHorizontal size={12} style={{ color: "#10B981" }} />
                              </button>
                              <button title="More" style={{ width: 27, height: 27, borderRadius: 7, border: "none", backgroundColor: "#F8FAFC", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center" }}>
                                <MoreHorizontal size={12} style={{ color: "#94A3B8" }} />
                              </button>
                            </div>
                          </td>
                          {/* Adjust hint */}
                          <td style={{ padding: "10px 6px" }} />
                        </tr>
                      );
                    })}
                  </tbody>
                );
              })}
            </table>

            {filtered.length === 0 && (
              <div style={{ padding: "48px", textAlign: "center" }}>
                <Package size={32} style={{ color: "#CBD5E1", margin: "0 auto 12px" }} />
                <p style={{ color: "#94A3B8", fontSize: "13px", margin: 0 }}>No items match your search.</p>
              </div>
            )}
          </div>

          {/* Footer */}
          <div style={{ padding: "11px 18px", borderTop: "1px solid #F1F5F9", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
            <span style={{ color: "#94A3B8", fontSize: "12px" }}>Showing {filtered.length} of {inventoryItems.length} items</span>
            <div style={{ display: "flex", alignItems: "center", gap: "4px" }}>
              {["‹ Prev", "1", "2", "Next ›"].map((p, i) => (
                <button key={i} style={{ minWidth: "28px", height: "28px", borderRadius: "7px", border: p === "1" ? "none" : "1px solid #E2E8F0", backgroundColor: p === "1" ? "#1E3A5F" : "transparent", color: p === "1" ? "#FFFFFF" : "#64748B", fontSize: "12px", fontWeight: p === "1" ? 700 : 400, cursor: "pointer", padding: "0 8px", fontFamily: "'Inter', sans-serif" }}>
                  {p}
                </button>
              ))}
            </div>
          </div>
        </div>

        {/* ── Right Panel ── */}
        <div style={{ width: "288px", minWidth: "288px", display: "flex", flexDirection: "column", gap: "14px" }}>

          {/* Stock Status Donut */}
          <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: "12px", padding: "18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
            <h4 style={{ margin: "0 0 12px", color: "#0F172A", fontSize: "13px", fontWeight: 700 }}>Stock Status</h4>
            <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
              <div style={{ position: "relative", flexShrink: 0 }}>
                <PieChart width={120} height={120}>
                  <Pie data={statusBreakdown.map(s => ({ ...s, fill: s.color }))} cx="50%" cy="50%" innerRadius={38} outerRadius={56} paddingAngle={3} dataKey="value" stroke="none" isAnimationActive={false} />
                  <Tooltip content={<CustomPieTooltip />} />
                </PieChart>
                <div style={{ position: "absolute", top: "50%", left: "50%", transform: "translate(-50%, -50%)", textAlign: "center", pointerEvents: "none" }}>
                  <div style={{ color: "#0F172A", fontSize: "14px", fontWeight: 800, lineHeight: 1 }}>{inventoryItems.length}</div>
                  <div style={{ color: "#94A3B8", fontSize: "8.5px", marginTop: "1px" }}>SKUs</div>
                </div>
              </div>
              <div style={{ flex: 1, display: "flex", flexDirection: "column", gap: "7px" }}>
                {statusBreakdown.map(s => (
                  <div key={s.name} style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                    <div style={{ display: "flex", alignItems: "center", gap: "6px" }}>
                      <div style={{ width: 8, height: 8, borderRadius: 2, backgroundColor: s.color, flexShrink: 0 }} />
                      <span style={{ color: "#64748B", fontSize: "11px" }}>{s.name}</span>
                    </div>
                    <span style={{ color: "#0F172A", fontSize: "12px", fontWeight: 700 }}>{s.value}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>

          {/* Reorder Alerts */}
          <div style={{ backgroundColor: "#FFFFFF", border: "1px solid #F1F5F9", borderRadius: "12px", padding: "18px", boxShadow: "0 1px 4px rgba(0,0,0,0.05)" }}>
            <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "12px" }}>
              <h4 style={{ margin: 0, color: "#0F172A", fontSize: "13px", fontWeight: 700 }}>Reorder Alerts</h4>
              <span style={{ backgroundColor: "#FEF2F2", color: "#EF4444", fontSize: "10px", fontWeight: 700, padding: "2px 7px", borderRadius: "10px" }}>
                {reorderAlerts.length} urgent
              </span>
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: "8px" }}>
              {reorderAlerts.map(item => {
                const catCfg = categoryConfig[item.category];
                const sCfg   = statusConfig[item.status];
                return (
                  <div key={item.sku} style={{ display: "flex", alignItems: "center", gap: "10px", padding: "9px 10px", borderRadius: "9px", backgroundColor: sCfg.bg, border: `1px solid ${sCfg.dot}22` }}>
                    <div style={{ width: 28, height: 28, borderRadius: 7, backgroundColor: catCfg.light, display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                      <span style={{ color: catCfg.color }}>{catCfg.icon}</span>
                    </div>
                    <div style={{ flex: 1, minWidth: 0 }}>
                      <div style={{ color: "#0F172A", fontSize: "11.5px", fontWeight: 600, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{item.name}</div>
                      <div style={{ color: "#94A3B8", fontSize: "10px" }}>{item.sku} · {item.inStock === 0 ? "No stock" : `${item.inStock} left`}</div>
                    </div>
                    <button onClick={() => setAdjustingItem(item)} title="Adjust"
                      style={{ width: 24, height: 24, borderRadius: 6, border: "none", backgroundColor: "rgba(255,255,255,0.7)", cursor: "pointer", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0 }}>
                      <SlidersHorizontal size={11} style={{ color: sCfg.dot }} />
                    </button>
                  </div>
                );
              })}
            </div>
          </div>

          {/* Top Items by Value */}
          <div style={{ backgroundColor: "#1E3A5F", border: "none", borderRadius: "12px", padding: "18px", boxShadow: "0 4px 16px rgba(30,58,95,0.25)" }}>
            <h4 style={{ margin: "0 0 14px", color: "#FFFFFF", fontSize: "13px", fontWeight: 700 }}>Top 5 by Value</h4>
            <div style={{ display: "flex", flexDirection: "column", gap: "10px" }}>
              {top5.map((item, idx) => {
                const pct = (item.totalValue / maxTopValue) * 100;
                const barColors = ["#3B82F6", "#10B981", "#8B5CF6", "#F59E0B", "#64748B"];
                return (
                  <div key={item.sku}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: "4px" }}>
                      <div style={{ display: "flex", alignItems: "center", gap: "6px", minWidth: 0 }}>
                        <span style={{ color: "rgba(255,255,255,0.4)", fontSize: "10px", fontWeight: 700, minWidth: "14px" }}>#{idx + 1}</span>
                        <span style={{ color: "rgba(255,255,255,0.85)", fontSize: "11px", fontWeight: 500, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", maxWidth: "148px" }}>{item.name}</span>
                      </div>
                      <span style={{ color: "#FFFFFF", fontSize: "11px", fontWeight: 700, flexShrink: 0 }}>
                        ${(item.totalValue / 1000).toFixed(1)}k
                      </span>
                    </div>
                    <div style={{ height: 4, backgroundColor: "rgba(255,255,255,0.1)", borderRadius: 2 }}>
                      <div style={{ height: "100%", width: `${pct}%`, backgroundColor: barColors[idx] ?? "#64748B", borderRadius: 2 }} />
                    </div>
                  </div>
                );
              })}
            </div>
            <div style={{ marginTop: "12px", paddingTop: "12px", borderTop: "1px solid rgba(255,255,255,0.1)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
              <span style={{ color: "rgba(255,255,255,0.5)", fontSize: "11px" }}>Total Inventory Value</span>
              <span style={{ color: "#FFFFFF", fontSize: "13px", fontWeight: 800 }}>${(totalValue / 1000).toFixed(1)}k</span>
            </div>
          </div>

        </div>
      </div>

      {/* ── Adjust Stock Modal ── */}
      {adjustingItem && (
        <AdjustStockModal
          item={adjustingItem}
          auditTrail={auditLog.filter(e => e.sku === adjustingItem.sku)}
          onClose={() => setAdjustingItem(null)}
          onConfirm={handleAdjust}
        />
      )}
    </div>
  );
}