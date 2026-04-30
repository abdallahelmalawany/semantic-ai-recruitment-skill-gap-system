import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity
from fastapi import FastAPI
from pydantic import BaseModel
import uvicorn

# ── Load & Prepare Data ──────────────────────────────
df = pd.read_csv("IT_Job_Roles_Skills.csv", encoding="latin1")
df["combined"] = df["Skills"].fillna("") + " " + df["Job Description"].fillna("")

# ── TF-IDF Vectorizer ────────────────────────────────
vectorizer = TfidfVectorizer(stop_words="english")
job_matrix = vectorizer.fit_transform(df["combined"])

# ── Match Function ───────────────────────────────────
def match_jobs(user_skills: str, top_n: int = 5):
    user_vec = vectorizer.transform([user_skills])
    scores = cosine_similarity(user_vec, job_matrix).flatten()
    top_indices = scores.argsort()[-top_n:][::-1]
    results = []
    for i in top_indices:
        results.append({
            "job_title": df.iloc[i]["Job Title"],
            "match_score": round(float(scores[i]) * 100, 2),
            "skills": df.iloc[i]["Skills"]
        })
    return results

# ── FastAPI App ──────────────────────────────────────
app = FastAPI()

class SkillsInput(BaseModel):
    skills: str

@app.post("/match")
def get_matches(input: SkillsInput):
    return {"matches": match_jobs(input.skills)}

@app.get("/health")
def health():
    return {"status": "ok", "jobs_loaded": len(df)}

# ── Test locally ─────────────────────────────────────
if __name__ == "__main__":
    print("\n🧪 Test Match:")
    results = match_jobs("Python, Machine Learning, SQL, TensorFlow")
    for r in results:
        print(f"  {r['match_score']}% → {r['job_title']}")
    
    print("\n🚀 Starting API server...")
    uvicorn.run(app, host="0.0.0.0", port=8000)