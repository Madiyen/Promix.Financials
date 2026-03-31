import React from "react";
import { TrendingUp, Clock, AlertTriangle, Wallet, ArrowUpRight, ArrowDownRight } from "lucide-react";

interface KPICardProps {
  title: string;
  value: string;
  subtitle: string;
  change: string;
  changePositive: boolean;
  icon: React.ReactNode;
  iconBg: string;
  iconColor: string;
  accentColor: string;
  sparkData: number[];
}

function MiniSparkline({ data, color }: { data: number[]; color: string }) {
  const max = Math.max(...data);
  const min = Math.min(...data);
  const range = max - min || 1;
  const width = 80;
  const height = 32;
  const points = data.map((v, i) => {
    const x = (i / (data.length - 1)) * width;
    const y = height - ((v - min) / range) * height;
    return `${x},${y}`;
  });

  return (
    <svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} style={{ overflow: "visible" }}>
      <polyline
        points={points.join(" ")}
        fill="none"
        stroke={color}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <polyline
        points={`0,${height} ${points.join(" ")} ${width},${height}`}
        fill={color}
        opacity="0.12"
        stroke="none"
      />
    </svg>
  );
}

function KPICard({ title, value, subtitle, change, changePositive, icon, iconBg, iconColor, accentColor, sparkData }: KPICardProps) {
  return (
    <div
      className="flex flex-col p-5 rounded-xl flex-1"
      style={{
        backgroundColor: "#FFFFFF",
        boxShadow: "0 1px 4px rgba(0,0,0,0.06), 0 4px 16px rgba(0,0,0,0.04)",
        border: "1px solid #F1F5F9",
        fontFamily: "'Inter', sans-serif",
        minWidth: 0,
        position: "relative",
        overflow: "hidden",
      }}
    >
      {/* Accent bar */}
      <div
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          right: 0,
          height: "3px",
          backgroundColor: accentColor,
          borderRadius: "12px 12px 0 0",
        }}
      />

      <div className="flex items-start justify-between mb-3">
        <div
          className="flex items-center justify-center rounded-xl"
          style={{
            width: "44px",
            height: "44px",
            backgroundColor: iconBg,
          }}
        >
          <span style={{ color: iconColor }}>{icon}</span>
        </div>
        <div className="flex items-center gap-1">
          <MiniSparkline data={sparkData} color={accentColor} />
        </div>
      </div>

      <div style={{ color: "#64748B", fontSize: "12px", fontWeight: 500, marginBottom: "4px" }}>
        {title}
      </div>
      <div style={{ color: "#0F172A", fontSize: "22px", fontWeight: 800, letterSpacing: "-0.5px", lineHeight: 1.1, marginBottom: "6px" }}>
        {value}
      </div>

      <div className="flex items-center justify-between mt-auto pt-3" style={{ borderTop: "1px solid #F1F5F9" }}>
        <span style={{ color: "#94A3B8", fontSize: "11px" }}>{subtitle}</span>
        <span
          className="flex items-center gap-1 px-2 py-0.5 rounded-full"
          style={{
            backgroundColor: changePositive ? "#ECFDF5" : "#FEF2F2",
            color: changePositive ? "#10B981" : "#EF4444",
            fontSize: "11px",
            fontWeight: 600,
          }}
        >
          {changePositive ? <ArrowUpRight size={11} /> : <ArrowDownRight size={11} />}
          {change}
        </span>
      </div>
    </div>
  );
}

const cards: KPICardProps[] = [
  {
    title: "Total Sales Today",
    value: "$24,580",
    subtitle: "vs. yesterday $21,200",
    change: "+15.9%",
    changePositive: true,
    icon: <TrendingUp size={22} />,
    iconBg: "#ECFDF5",
    iconColor: "#10B981",
    accentColor: "#10B981",
    sparkData: [12, 18, 15, 22, 19, 26, 24],
  },
  {
    title: "Accounts Receivable",
    value: "$87,340",
    subtitle: "42 invoices pending",
    change: "+6.2%",
    changePositive: true,
    icon: <Clock size={22} />,
    iconBg: "#FFF7ED",
    iconColor: "#F59E0B",
    accentColor: "#F59E0B",
    sparkData: [60, 72, 68, 80, 75, 88, 87],
  },
  {
    title: "Accounts Payable",
    value: "$31,920",
    subtitle: "18 bills overdue",
    change: "-3.4%",
    changePositive: false,
    icon: <AlertTriangle size={22} />,
    iconBg: "#FEF2F2",
    iconColor: "#EF4444",
    accentColor: "#EF4444",
    sparkData: [38, 34, 40, 36, 33, 31, 32],
  },
  {
    title: "Cash & Bank Balance",
    value: "$156,740",
    subtitle: "3 bank accounts",
    change: "+8.7%",
    changePositive: true,
    icon: <Wallet size={22} />,
    iconBg: "#EFF6FF",
    iconColor: "#3B82F6",
    accentColor: "#3B82F6",
    sparkData: [120, 130, 128, 140, 145, 152, 156],
  },
];

export function KPICards() {
  return (
    <div className="flex gap-4">
      {cards.map((card) => (
        <KPICard key={card.title} {...card} />
      ))}
    </div>
  );
}