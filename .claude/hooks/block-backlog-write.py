#!/usr/bin/env python3
"""PreToolUse : interdit d'inscrire un reste-à-faire (backlog, agenda) sans accord.

Le backlog est la liste de DETTE ACCEPTÉE de l'humain : y écrire, c'est décider à
sa place qu'un défaut ne sera pas corrigé. Signaler après n'est pas demander. Une
règle écrite en prose s'érode ; ce qu'une machine applique tient.
"""
import json
import re
import sys

# La dette vit dans le graphe (`query.mjs --add backlog …`). Le chemin fichier est
# gardé par sécurité : si quelqu'un crée `docs/backlog.md`, il est protégé.
CIBLE = "docs/backlog.md"
# Écriture d'une dette dans le graphe, par le client ou par l'outil MCP.
# 🔴 `(?![-\w])` et NON `\b` : `-` n'est pas un caractère de mot, donc `backlog\b`
# matcherait aussi `--add backlog-résolu` — SOLDER une dette serait bloqué comme si on
# en inscrivait une neuve.
ECRITURE_GRAPHE = re.compile(
    r"query\.mjs\b.*--add\s+backlog(?![-\w])|--add\s+backlog(?![-\w]).*query\.mjs",
    re.IGNORECASE)
# Règle de l'humain : « arrête d'ajouter des restes à faire ». L'agenda serait sinon
# le contournement : `--add agenda` rangerait un reste-à-faire sans frottement.
# Même garde que ci-dessus : le type exact, jamais un type dérivé.
ECRITURE_AGENDA = re.compile(
    r"query\.mjs\b.*--add\s+agenda(?![-\w])|--add\s+agenda(?![-\w]).*query\.mjs",
    re.IGNORECASE)
# Seule sortie : la CLÔTURE de session, où l'humain a lui-même dit « fin » ou lancé
# `/status`. `session-closer` doit alors réécrire le pointeur d'agenda. Le hook ne
# peut pas reconnaître l'agent appelant, donc l'exception est déclarative :
# `SP_CLOTURE=1 node scripts/memory/query.mjs --add agenda …`. C'est un ralentisseur,
# pas un verrou — il force à nommer l'intention, il n'empêche personne de mentir.
EXEMPTION_CLOTURE = re.compile(r"\bSP_CLOTURE=1\b")
# Outils MCP mémoire qui ne peuvent RIEN inscrire. Les refuser sur la simple présence
# du mot « backlog » empêchait de RELIRE une dette, y compris une dette déjà soldée —
# un garde d'écriture qui bloquait une lecture.
MCP_LECTURE_SEULE = ("open_nodes", "search_nodes", "read_graph")
# Le type exact d'une entité inscrite par un outil MCP.
TYPE_ENTITE = re.compile(r'"entityType"\s*:\s*"([^"]*)"')
TYPES_INTERDITS = ("backlog", "agenda")


def inscrit_une_dette(entree):
    """Vrai si l'entrée crée une entité typée EXACTEMENT `backlog` ou `agenda`.

    `backlog-résolu` n'en est pas une : solder une dette est le geste inverse de
    l'inscrire, et c'est ce que l'ancienne recherche de sous-chaîne confondait.
    """
    charge = json.dumps(entree, ensure_ascii=False)
    types = TYPE_ENTITE.findall(charge)
    if types:
        return any(t.lower() in TYPES_INTERDITS for t in types)
    # Forme inattendue : on ne sait pas lire, donc on refuse. Le garde prime sur le
    # confort.
    return "backlog" in charge.lower()
MESSAGE = (
    "Enregistrement d'une dette bloqué : le backlog est la liste de dette ACCEPTÉE "
    "par l'humain. Y ajouter une entrée revient à décider à sa place qu'un défaut "
    "ne sera pas corrigé.\n\n"
    "Le geste attendu, en chat, AVANT toute écriture : « j'ai trouvé <le défaut> — "
    "je le corrige maintenant, ou je le range au backlog ? Tu choisis. »\n\n"
    "Vaut aussi pour un défaut pré-existant que tu n'as pas introduit. Si l'humain "
    "a déjà donné son accord explicite dans "
    "cette conversation, dis-le lui et demande-lui de relancer l'action."
)


MESSAGE_AGENDA = (
    "Enregistrement d'un reste-à-faire bloqué. Règle de l'humain : "
    "« arrête d'ajouter des restes à faire ; si tu trouves un truc en route, on en "
    "discute ». Rien au backlog NI à l'agenda sans son accord explicite.\n\n"
    "Le geste attendu, en chat, AVANT toute écriture : « j'ai trouvé <le défaut> — "
    "besoin de ta décision, ou j'avance ? »\n\n"
    "Exception unique — la CLÔTURE de session, quand l'humain a dit « fin » ou lancé "
    "`/status` : relancer la commande préfixée de `SP_CLOTURE=1`."
)


def refuse(message=None):
    print(json.dumps({"hookSpecificOutput": {
        "hookEventName": "PreToolUse",
        "permissionDecision": "deny",
        "permissionDecisionReason": message or MESSAGE,
    }}))


def main():
    charge = json.load(sys.stdin)
    outil = charge.get("tool_name") or ""
    entree = charge.get("tool_input") or {}

    if outil == "Bash":
        commande = str(entree.get("command", ""))
        if ECRITURE_GRAPHE.search(commande):
            refuse()
        elif ECRITURE_AGENDA.search(commande) and not EXEMPTION_CLOTURE.search(commande):
            refuse(MESSAGE_AGENDA)
        return
    if outil.startswith("mcp__memory__"):
        if outil.endswith(MCP_LECTURE_SEULE):
            return
        if inscrit_une_dette(entree):
            refuse()
        return

    chemins = [str(entree.get(c, "")) for c in ("file_path", "path", "notebook_path")]
    if not any(CIBLE in c for c in chemins):
        return
    refuse()


try:
    main()
except Exception:
    pass          # un hook défaillant ne doit pas bloquer un outil légitime
sys.exit(0)
