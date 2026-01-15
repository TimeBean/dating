
#!/usr/bin/env python3
"""
graph.py — парсер DialogSteps (.cs) -> интерактивный HTML graph (+ optional PNG)

Fixes:
- корректно различает guard vs callback conditions (не считает Callback.Data за guard)
- сохраняет и показывает preview с переносами в info panel (<pre>)
- короткая метка условия показывается над стрелкой (label)
"""
import os, re, sys, argparse, json, subprocess, html
from collections import defaultdict, deque

# ---------------- regex ----------------
STATE_PROP_RE = re.compile(r"State\s*=>\s*DialogState\.([A-Za-z0-9_]+)")
SET_STATE_RE   = re.compile(r"session\.State\s*=\s*DialogState\.([A-Za-z0-9_]+)")
IF_RE          = re.compile(r"if\s*\((.*?)\)\s*{", re.S)
CALLBACK_RE    = re.compile(r'CallbackQuery!?\.Data\s*==\s*"([^"]+)"', re.S)

# ---------------- helpers ----------------
def find_matching_brace(text, start_idx):
    assert text[start_idx] == '{'
    depth = 0
    for i in range(start_idx, len(text)):
        c = text[i]
        if c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                return i
    return -1

def safe_label(s):
    if s is None: return ""
    return html.escape(str(s).replace('"', "'").strip())

def is_guard(cond):
    """Более точная детекция guard-условий.

    Считаем guard, если условие:
      - проверяет update.Type или update.Message (тип/наличие),
      - проверяет на null / == null / is null,
      - либо содержит 'return' (однострочный guard)
    Не считаем guard если условие содержит проверку .Data (т.е. конкретный callback).
    """
    if not cond: 
        return False
    c = cond.strip()
    low = c.lower()
    # quick guard patterns
    if "return" in low:
        return True
    if "== null" in low or "is null" in low or "is not null" in low:
        return True
    if "update.type" in low or "update.message" in low:
        return True
    # special-case: if it mentions CallbackQuery but also .data or data== then it's NOT a guard
    if "callbackquery" in low:
        if "data" in low:
            return False
        return True
    return False

def extract_callback_from_text(text):
    m = CALLBACK_RE.search(text)
    return m.group(1) if m else None

def find_last_relevant_if(snippet):
    ifs = list(IF_RE.finditer(snippet))
    for m in reversed(ifs):
        cond = m.group(1).strip()
        if not is_guard(cond):
            return cond
    return None

# ---------------- parsing one file ----------------
def parse_handle_blocks(text):
    m = re.search(r"HandleAsync\s*\(.*?\)\s*{", text, re.S)
    if not m:
        return []
    body_start = text.find('{', m.end()-1)
    if body_start == -1:
        return []
    body_end = find_matching_brace(text, body_start)
    if body_end == -1:
        return []
    body = text[body_start+1:body_end]
    base = body_start+1
    blocks = []
    for im in IF_RE.finditer(body):
        cond = im.group(1).strip()
        brace_open = body.find('{', im.end()-1)
        if brace_open == -1:
            continue
        global_open = base + brace_open
        global_close = find_matching_brace(text, global_open)
        if global_close == -1:
            continue
        blk_code = text[global_open+1:global_close]
        blocks.append({
            "start": global_open,
            "end": global_close,
            "cond": cond,
            "code": blk_code
        })
    # top-level
    blocks.append({
        "start": base,
        "end": body_end,
        "cond": None,
        "code": text[base:body_end]
    })
    blocks.sort(key=lambda b: (b['start'], -(b['end']-b['start'])))
    return blocks

def parse_cs_file(path):
    try:
        text = open(path, encoding='utf-8').read()
    except Exception as e:
        print(f"Warning: cannot read {path}: {e}", file=sys.stderr)
        return []
    sm = STATE_PROP_RE.search(text)
    if not sm:
        return []
    from_state = sm.group(1)
    blocks = parse_handle_blocks(text)
    edges = []
    for m in SET_STATE_RE.finditer(text):
        to_state = m.group(1)
        pos = m.start()
        chosen = None
        for b in blocks:
            if b['start'] <= pos <= b['end']:
                if chosen is None or (b['end']-b['start']) < (chosen['end']-chosen['start']):
                    chosen = b
        cond_label = "always"
        cb_label = None
        if chosen:
            if chosen['cond'] and not is_guard(chosen['cond']):
                cond_label = chosen['cond']
            cb_label = extract_callback_from_text(chosen['code'])
        # preview: keep original newlines and indenting (trim to window)
        start_preview = max(0, pos - 300)
        end_preview = min(len(text), pos + 300)
        preview_raw = text[start_preview:end_preview].rstrip()
        edges.append({
            "from": from_state,
            "to": to_state,
            "cond": cond_label,
            "callback": cb_label,
            "file": os.path.basename(path),
            "preview": preview_raw
        })
    return edges

# ---------------- build graph ----------------
def build_graph(dir_path):
    nodes = set()
    edges = []
    if not os.path.isdir(dir_path):
        raise FileNotFoundError(dir_path)
    for root, _, files in os.walk(dir_path):
        for fn in sorted(files):
            if not fn.endswith(".cs"): continue
            p = os.path.join(root, fn)
            found = parse_cs_file(p)
            for e in found:
                nodes.add(e['from'])
                nodes.add(e['to'])
                edges.append(e)
    return nodes, edges

# ---------------- layout ----------------
def layout_nodes(nodes, edges, x_spacing=260, y_spacing=160):
    indeg = defaultdict(int)
    children = defaultdict(list)
    for e in edges:
        children[e['from']].append(e['to'])
        indeg[e['to']] += 1
        indeg.setdefault(e['from'], indeg.get(e['from'],0))
    roots = [n for n in nodes if indeg.get(n,0)==0]
    if not roots:
        roots = [next(iter(nodes))]
    level = {}
    q = deque()
    for r in roots:
        level[r]=0
        q.append(r)
    while q:
        u = q.popleft()
        for v in children.get(u,[]):
            nl = level.get(v, None)
            if nl is None or nl > level[u]+1:
                level[v] = level[u]+1
                q.append(v)
    max_level = max(level.values()) if level else 0
    for n in nodes:
        if n not in level:
            max_level += 1
            level[n] = max_level
    levels = defaultdict(list)
    for n,l in level.items():
        levels[l].append(n)
    positions = {}
    current_x = 0
    for lvl in sorted(levels.keys()):
        nlist = sorted(levels[lvl], key=lambda x: str(x))
        for n in nlist:
            positions[n] = (current_x, lvl * y_spacing)
            current_x += x_spacing
        current_x += x_spacing//2
    return positions

# ---------------- UI helpers ----------------
def short_cond(cond, max_len=40):
    if not cond or cond == "always":
        return ""
    cond = cond.strip()
    if len(cond) <= max_len:
        return cond
    return cond[:max_len//2] + "..." + cond[-max_len//2:]

# ---------------- export HTML ----------------
def export_html(nodes, edges, positions, out_html="dialog_graph.html"):
    node_items = []
    for n in sorted(nodes):
        is_end = str(n).lower() == "none"
        lbl = safe_label(n)
        color_bg = "#ffecec" if is_end else "#eaf7ea"
        border = "#ff6b6b" if is_end else "#2e7d32"
        shape = "ellipse" if is_end else "box"
        x,y = positions.get(n, (0,0))
        node_items.append({
            "id": lbl,
            "label": lbl,
            "x": x,
            "y": y,
            "color": {"background": color_bg, "border": border},
            "shape": shape
        })
    edge_items = []
    for idx, e in enumerate(edges):
        frm = safe_label(e['from'])
        to  = safe_label(e['to'])
        cond = e['cond']
        cb = e['callback']
        label_short = ""
        full_label = ""
        color_val = "#555"
        dashes = False

        if cb:
            label_short = short_cond(f'callback: {cb}', 40)
            full_label = f'Callback: {cb}'
            color_val = "#1976d2"
        elif cond and cond != "always":
            label_short = short_cond(cond, 40)
            full_label = cond
            color_val = "#6a1b9a"
        else:
            label_short = ""
            full_label = ""
            color_val = "#888"
            dashes = True

        # preview: escape html but keep newlines -> show in <pre>
        preview_html = html.escape(e.get("preview",""))
        edge_items.append({
            "id": f"e{idx}",
            "from": frm,
            "to": to,
            "label": label_short,
            "fullLabel": full_label,
            "file": e["file"],
            "preview": preview_html,
            "color": {"color": color_val},
            "dashes": dashes,
            "arrows": "to",
            "font": {"size": 14, "align": "top", "vadjust": -16, "color": "#222"}
        })

    html_doc = f"""<!doctype html>
<html>
<head><meta charset="utf-8"><title>Dialog Graph</title>
<script src="https://unpkg.com/vis-network/standalone/umd/vis-network.min.js"></script>
<style>
  html,body,#network{{height:100%;width:100%;margin:0;padding:0}}
  #infoPanel{{position:fixed;right:12px;bottom:12px;width:480px;max-height:70%;overflow:auto;background:#fff;border:1px solid #ddd;border-radius:8px;padding:12px;font-family:Arial;font-size:13px;box-shadow:0 8px 24px rgba(0,0,0,0.08)}}
  #infoPanel h3{{margin:6px 0 8px 0;font-size:15px}}
  #infoPanel pre{{background:#f6f8fa;padding:8px;border-radius:6px;overflow:auto;font-size:12px;white-space:pre-wrap}}
  .muted{{color:#666;font-size:12px}}
  .legend{{position:absolute;right:12px;top:12px;background:#fff;padding:10px;border-radius:8px;border:1px solid #ddd;font-family:Arial;font-size:13px}}
</style>
</head>
<body>
<div id="network"></div>

<div id="infoPanel"><h3>Информация</h3><div class="muted">Нажмите на стрелку (edge), чтобы увидеть полное условие, файл и форматированный фрагмент кода.</div></div>

<div class="legend">
  <div><span style="display:inline-block;width:14px;height:12px;background:#eaf7ea;border:1px solid #2e7d32;margin-right:8px;"></span> Normal state</div>
  <div><span style="display:inline-block;width:14px;height:12px;background:#ffecec;border:1px solid #ff6b6b;margin-right:8px;"></span> End (None)</div>
  <div><span style="display:inline-block;width:14px;height:12px;background:#1976d2;margin-right:8px;"></span> Callback edge</div>
  <div><span style="display:inline-block;width:14px;height:12px;background:#6a1b9a;margin-right:8px;"></span> Conditional edge</div>
  <div><span style="display:inline-block;width:14px;height:12px;background:#888;margin-right:8px;"></span> Always (dashed)</div>
</div>

<script>
const nodes = new vis.DataSet({json.dumps(node_items, ensure_ascii=False)});
const edges = new vis.DataSet({json.dumps(edge_items, ensure_ascii=False)});
const container = document.getElementById('network');
const data = {{nodes, edges}};
const options = {{
  physics: false,
  interaction: {{ hover: false, navigationButtons: true, keyboard: true }},
  nodes: {{ shape: 'box', margin: 10, font: {{ size: 14, face: 'Arial' }} }},
  edges: {{ smooth: {{enabled:true}}, font: {{size:14, color:'#222', align:'top', vadjust:-16}} }},
}};
const network = new vis.Network(container, data, options);
network.fit();

// click -> show formatted info in right-bottom panel
network.on('click', function(params) {{
  const info = document.getElementById('infoPanel');
  if (params.edges && params.edges.length > 0) {{
    const eid = params.edges[0];
    const ed = edges.get(eid);
    let html = '<h3>Условие</h3>';
    if (ed.fullLabel && ed.fullLabel.length>0) {{
      html += '<div style="font-weight:600;margin-bottom:6px;">' + ed.fullLabel + '</div>';
    }} else {{
      html += '<div class="muted">(нет условия — always)</div>';
    }}
    html += '<h3>Файл</h3>';
    html += '<div class="muted">' + (ed.file || '') + '</div>';
    html += '<h3>Фрагмент кода</h3>';
    html += '<pre>' + (ed.preview || '') + '</pre>';
    info.innerHTML = html;
  }} else {{
    info.innerHTML = '<h3>Информация</h3><div class="muted">Нажмите на стрелку (edge), чтобы увидеть полное условие, файл и форматированный фрагмент кода.</div>';
  }}
}});
</script>
</body></html>
"""
    with open(out_html, "w", encoding="utf-8") as fh:
        fh.write(html_doc)
    print(f"HTML saved to: {out_html}")

# ---------------- export DOT (optional) ----------------
def export_dot(nodes, edges, out_dot="dialog_graph.dot"):
    with open(out_dot, "w", encoding="utf-8") as f:
        f.write("digraph Dialog { rankdir=TB; node [fontname=\"Arial\"]; \n")
        for n in sorted(nodes):
            attr = 'shape=ellipse, style=filled, fillcolor=lightcoral, color=red' if str(n).lower()=="none" else 'shape=box'
            f.write(f'  "{n}" [{attr}];\n')
        for e in edges:
            frm = e['from']; to = e['to']
            if e['callback']:
                f.write(f'  "{frm}" -> "{to}" [color=blue, label="callback: {e["callback"]}"];\n')
            elif e['cond'] and e['cond'] != "always":
                lab = e['cond'].replace('"', "'")
                f.write(f'  "{frm}" -> "{to}" [color=purple, label="{lab}"];\n')
            else:
                f.write(f'  "{frm}" -> "{to}" [color=gray, style=dashed];\n')
        f.write("}\n")
    return out_dot

def run_dot(dot, png):
    try:
        subprocess.run(["dot","-Tpng",dot,"-o",png], check=True)
        print(f"PNG saved to: {png}")
    except FileNotFoundError:
        print("Graphviz 'dot' not found; install graphviz to enable --png.")
    except subprocess.CalledProcessError as e:
        print("dot failed:", e)

# ---------------- CLI ----------------
def main():
    p = argparse.ArgumentParser(description="DialogSteps -> interactive HTML graph (+ optional PNG)")
    p.add_argument("path", help="Directory with DialogSteps (.cs files)")
    p.add_argument("--output", "-o", default="dialog_graph.html")
    p.add_argument("--png", action="store_true", help="also render PNG (requires graphviz)")
    args = p.parse_args()

    nodes, edges = build_graph(args.path)
    if not nodes:
        print("No states found. Check path and .cs files.", file=sys.stderr)
        sys.exit(1)

    positions = layout_nodes(nodes, edges)
    export_html(nodes, edges, positions, args.output)
    if args.png:
        dot = export_dot(nodes, edges)
        run_dot(dot, "dialog_graph.png")

if __name__ == "__main__":
    main()
