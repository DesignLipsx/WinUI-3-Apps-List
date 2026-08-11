import os
import re
import json
import io
import urllib.request
from concurrent.futures import ThreadPoolExecutor, as_completed
from PIL import Image
import resvg_py

ASSETS_APPS_DIR = os.path.join('FluentDeck', 'FluentDeck', 'Assets', 'apps')

def clean_html_and_emoji(text):
    if not text:
        return ""
    cleaned = re.sub(r'<[^>]+>', '', text)
    return cleaned.strip()

def slugify(text):
    text = text.lower()
    text = re.sub(r'[^a-z0-9]+', '-', text)
    return text.strip('-')

def is_svg_data(data, url):
    if url.lower().endswith('.svg'):
        return True
    try:
        head = data[:500].decode('utf-8', errors='ignore').strip()
        if '<svg' in head.lower() or ('<?xml' in head.lower() and 'svg' in head.lower()):
            return True
    except Exception:
        pass
    return False

def download_and_convert_logo(url, slug, output_dir=ASSETS_APPS_DIR):
    if not url or not url.startswith('http') or not slug:
        return url

    os.makedirs(output_dir, exist_ok=True)
    filename = f"{slug}.webp"
    webp_path = os.path.join(output_dir, filename)
    asset_uri = f"/assets/apps/{filename}"

    if os.path.exists(webp_path) and os.path.getsize(webp_path) > 0:
        return asset_uri

    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = resp.read()

        if is_svg_data(data, url):
            try:
                svg_str = data.decode('utf-8', errors='ignore')
                png_bytes = resvg_py.svg_to_bytes(svg_str, width=64, height=64)
                img = Image.open(io.BytesIO(png_bytes))
            except Exception as svg_err:
                print(f"SVG render error for {slug}: {svg_err}")
                return url
        else:
            img = Image.open(io.BytesIO(data))

            # Handle ICO or multi-frame images
            best_frame = img
            max_area = 0
            if getattr(img, "n_frames", 1) > 1:
                for frame_idx in range(img.n_frames):
                    try:
                        img.seek(frame_idx)
                        f = img.copy()
                        if f.width * f.height > max_area:
                            max_area = f.width * f.height
                            best_frame = f
                    except Exception:
                        break
                img = best_frame

        if img.mode != 'RGBA':
            img = img.convert('RGBA')

        img.thumbnail((64, 64), Image.Resampling.LANCZOS)
        img.save(webp_path, 'WEBP', quality=80)
        return asset_uri
    except Exception as e:
        print(f"Failed logo for {slug} ({url}): {e}")
        return url

def parse_app_line(line, logos_dict):
    line = line.strip()
    if not line.startswith('-'):
        return None

    # Example lines:
    # - `WDM` [Awesome Media Player WinUI3](https://github.com/bluday/awesome-media-player) `📆` <sup>`FOSS`</sup>
    # - `WD` [Danmaku Player](https://github.com/Poker-sang/DanmakuPlayer) <sup>`FOSS`</sup> <!-- logo: ... -->
    
    # 1. Extract indicator if present: `WDM` or `WD`
    indicator = ""
    ind_match = re.search(r'`([A-Z0-9_\-]+)`', line)
    if ind_match:
        indicator = ind_match.group(1)

    # 2. Extract name & url: [Name](Url)
    link_match = re.search(r'\[([^\]]+)\]\((https?://[^\)]+)\)', line)
    if not link_match:
        return None

    name = link_match.group(1).strip()
    url = link_match.group(2).strip()

    # 3. Extract logo from comment if present <!-- logo: ... --> or from logos_dict
    logo = ""
    comment_logo_match = re.search(r'<!--\s*logo:\s*(https?://[^\s>]+)\s*-->', line, re.IGNORECASE)
    if comment_logo_match:
        logo = comment_logo_match.group(1).strip()
    elif url in logos_dict:
        logo = logos_dict[url]

    # 4. Extract flags
    is_foss = bool(re.search(r'<sup>`?FOSS`?</sup>|FOSS', line, re.IGNORECASE))
    is_paid = '💰' in line
    is_planned = '📆' in line or 'planned' in line.lower()
    is_discontinued = '❌' in line or '❎' in line or 'discontinued' in line.lower()
    is_theme = '🎨' in line or 'theme' in line.lower()

    return {
        "name": name,
        "url": url,
        "indicator": indicator,
        "logo": logo,
        "isFoss": is_foss,
        "isPaid": is_paid,
        "isPlanned": is_planned,
        "isDiscontinued": is_discontinued,
        "isTheme": is_theme
    }

def main():
    readme_path = 'README.md'
    output_json_path = 'apps_data.json'

    # Load existing logos map from apps_data.json if present
    logos_dict = {} # url -> logo_url
    if os.path.exists(output_json_path):
        try:
            with open(output_json_path, 'r', encoding='utf-8') as f:
                existing_data = json.load(f)
                def extract_logos(obj):
                    if isinstance(obj, dict):
                        if 'url' in obj and 'logo' in obj and obj['logo']:
                            logos_dict[obj['url']] = obj['logo']
                        for v in obj.values():
                            extract_logos(v)
                    elif isinstance(obj, list):
                        for item in obj:
                            extract_logos(item)
                extract_logos(existing_data)
        except Exception:
            pass

    if not os.path.exists(readme_path):
        print(f"Error: {readme_path} not found.")
        return

    with open(readme_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    categories_tree = []
    
    current_h2 = None
    current_h3 = None
    current_h4 = None

    # Sections outside Apps List main hierarchy
    best_implementation = []
    newly_added = []

    in_apps_list = False
    in_best_impl = False

    for line in lines:
        raw_line = line.rstrip('\r\n')
        trimmed = raw_line.strip()
        if not trimmed:
            continue

        if trimmed.startswith('#'):
            m = re.match(r'^(#+)\s*(.*)', trimmed)
            if not m:
                continue
            level = len(m.group(1))
            header_raw = m.group(2)
            header_clean = clean_html_and_emoji(header_raw)

            if level == 1 and 'Apps List' in header_clean:
                in_apps_list = True
                in_best_impl = False
                continue

            if 'Best Implementation of WinUI' in header_clean:
                in_best_impl = True
                in_apps_list = False
                continue

            if in_best_impl:
                if level <= 2 and 'Best Implementation' not in header_clean:
                    in_best_impl = False

            if in_apps_list or in_best_impl:
                if level == 2:
                    current_h2 = {
                        "name": header_clean,
                        "rawName": header_raw,
                        "subcategories": [],
                        "apps": []
                    }
                    current_h3 = None
                    current_h4 = None
                    if in_apps_list:
                        categories_tree.append(current_h2)
                elif level == 3 and current_h2:
                    current_h3 = {
                        "name": header_clean,
                        "rawName": header_raw,
                        "subcategories": [],
                        "apps": []
                    }
                    current_h4 = None
                    current_h2["subcategories"].append(current_h3)
                elif level == 4 and current_h3:
                    current_h4 = {
                        "name": header_clean,
                        "rawName": header_raw,
                        "apps": []
                    }
                    current_h3["subcategories"].append(current_h4)

        elif trimmed.startswith('-'):
            app_obj = parse_app_line(trimmed, logos_dict)
            if app_obj:
                if in_best_impl:
                    best_implementation.append(app_obj)
                elif in_apps_list:
                    if current_h2 and current_h2["name"] == "Newly Added Apps!":
                        newly_added.append(app_obj)
                        current_h2["apps"].append(app_obj)
                    elif current_h4:
                        current_h4["apps"].append(app_obj)
                    elif current_h3:
                        current_h3["apps"].append(app_obj)
                    elif current_h2:
                        current_h2["apps"].append(app_obj)

    # Collect all unique app logos to download & convert to WebP
    all_app_objects = []
    def collect_apps(node):
        if "apps" in node and node["apps"]:
            all_app_objects.extend(node["apps"])
        if "subcategories" in node and node["subcategories"]:
            for sub in node["subcategories"]:
                collect_apps(sub)

    for cat in categories_tree:
        collect_apps(cat)
    all_app_objects.extend(best_implementation)

    unique_logo_tasks = {}
    for app in all_app_objects:
        logo_url = app.get("logo", "")
        app_name = app.get("name", "")
        if logo_url and logo_url.startswith("http"):
            slug = slugify(app_name)
            if slug:
                unique_logo_tasks[logo_url] = (logo_url, slug)

    logo_url_to_converted = {}
    if unique_logo_tasks:
        print(f"Converting {len(unique_logo_tasks)} logos (including SVG, ICO, PNG, JPG) to WebP format...")
        with ThreadPoolExecutor(max_workers=10) as executor:
            future_to_url = {
                executor.submit(download_and_convert_logo, url, slug): url
                for url, slug in unique_logo_tasks.values()
            }
            for future in as_completed(future_to_url):
                url = future_to_url[future]
                try:
                    converted_uri = future.result()
                    if converted_uri:
                        logo_url_to_converted[url] = converted_uri
                except Exception:
                    pass

    for app in all_app_objects:
        orig_logo = app.get("logo", "")
        if orig_logo in logo_url_to_converted:
            app["logo"] = logo_url_to_converted[orig_logo]

    seen_names = set()
    seen_urls = set()
    unique_count = 0

    def process_node_count(node):
        nonlocal unique_count
        if "apps" in node and node["apps"]:
            for app in node["apps"]:
                n = app.get("name", "").strip().lower()
                u = app.get("url", "").strip().lower()
                if not n or not u:
                    continue
                if n in seen_names or u in seen_urls:
                    continue
                seen_names.add(n)
                seen_urls.add(u)
                unique_count += 1

        if "subcategories" in node and node["subcategories"]:
            for sub in node["subcategories"]:
                process_node_count(sub)

    for cat in categories_tree:
        if cat.get("name") != "Newly Added Apps!":
            process_node_count(cat)

    output_data = {
        "version": "1.0",
        "totalCount": unique_count,
        "bestImplementation": best_implementation,
        "newlyAdded": newly_added,
        "categories": categories_tree
    }

    output_json_paths = [output_json_path, os.path.join('FluentDeck', 'FluentDeck', 'Assets', 'data', 'apps_data.json')]
    for path in output_json_paths:
        parent = os.path.dirname(path)
        if parent and not os.path.exists(parent):
            os.makedirs(parent, exist_ok=True)
        with open(path, 'w', encoding='utf-8') as f:
            json.dump(output_data, f, indent=2, ensure_ascii=False)

    total_apps_count = sum(len(c.get("apps", [])) + sum(len(s.get("apps", [])) + sum(len(ss.get("apps", [])) for ss in s.get("subcategories", [])) for s in c.get("subcategories", [])) for c in categories_tree)
    print(f"Generated apps_data.json successfully across project assets!")
    print(f"Total categories: {len(categories_tree)}")
    print(f"Total apps in hierarchy: {total_apps_count}")
    print(f"Total unique apps: {unique_count}")
    print(f"Best Implementation apps: {len(best_implementation)}")

if __name__ == '__main__':
    main()
