using System.Globalization;
using System.Text.RegularExpressions;

namespace CalculatriceMAUI;

public partial class MainPage : ContentPage
{
    private string _expr = "";
    private string _resultat = "0";
    private bool _apresEgal;
    private bool _erreur;
    private bool _degres = true;
    private bool? _paysage;

    private static readonly string[] Fonctions = { "sin(", "cos(", "tan(", "log(", "ln(", "√(" };
    private const string Operateurs = "+-×÷^";

    private double TailleNormale => _paysage == true ? 34 : 52;

    public MainPage()
    {
        InitializeComponent();

        ScrollOperation.SizeChanged += (s, e) =>
        {
            if (ScrollOperation.Width > 0) LblOperation.MinimumWidthRequest = ScrollOperation.Width;
        };
        ScrollResultat.SizeChanged += (s, e) =>
        {
            if (ScrollResultat.Width > 0) LblResultat.MinimumWidthRequest = ScrollResultat.Width;
        };
    }

    // Adaptation portrait / paysage
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width <= 0 || height <= 0) return;

        bool paysage = width > height;
        if (_paysage == paysage) return;
        _paysage = paysage;

        Clavier.RowDefinitions.Clear();
        Clavier.ColumnDefinitions.Clear();

        if (paysage)
        {
            Clavier.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Clavier.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Clavier.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Grid.SetRow(PanneauSci, 0); Grid.SetColumn(PanneauSci, 0);
            Grid.SetRow(PanneauNum, 0); Grid.SetColumn(PanneauNum, 1);
            Racine.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
            Racine.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);
        }
        else
        {
            Clavier.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Clavier.RowDefinitions.Add(new RowDefinition(new GridLength(3, GridUnitType.Star)));
            Clavier.RowDefinitions.Add(new RowDefinition(new GridLength(5, GridUnitType.Star)));
            Grid.SetRow(PanneauSci, 0); Grid.SetColumn(PanneauSci, 0);
            Grid.SetRow(PanneauNum, 1); Grid.SetColumn(PanneauNum, 0);
            Racine.RowDefinitions[0].Height = new GridLength(2, GridUnitType.Star);
            Racine.RowDefinitions[1].Height = new GridLength(5, GridUnitType.Star);
        }

        MettreAJour();
    }

    // Gestion des touches
    private void OnModeAngle(object sender, EventArgs e)
    {
        _degres = !_degres;
        BtnAngle.Text = _degres ? "DEG" : "RAD";
        MettreAJour();
    }

    private void OnTouche(object sender, EventArgs e)
    {
        string t = ((Button)sender).Text;
        if (_erreur) Reinitialiser();

        switch (t)
        {
            case "AC": Reinitialiser(); break;
            case "DEL": Effacer(); break;
            case "=": Egal(); return;
            case "+": case "-": case "×": case "÷": AjouterOperateur(t); break;
            case "xʸ": AjouterOperateur("^"); break;
            case "x²": AjouterPostfixe("^2"); break;
            case "1/x": AjouterPostfixe("^(-1)"); break;
            case "n!": AjouterPostfixe("!"); break;
            case ",": AjouterVirgule(); break;
            case "(": Inserer("("); break;
            case ")": FermerParenthese(); break;
            case "π": Inserer("π"); break;
            case "e": Inserer("e"); break;
            case "10ˣ": Inserer("10^("); break;
            case "√": Inserer("√("); break;
            case "sin":
            case "cos":
            case "tan":
            case "ln":
            case "log":
                Inserer(t + "("); break;
            default:
                if (t.Length == 1 && char.IsDigit(t[0])) AjouterChiffre(t[0]);
                break;
        }
        MettreAJour();
    }

    // Gestion après égal
    private void Debut(bool continuer)
    {
        if (!_apresEgal) return;
        _apresEgal = false;
        _expr = continuer
            ? (_resultat.StartsWith('-') ? $"({_resultat})" : _resultat)
            : "";
    }

    private void Inserer(string s)
    {
        Debut(false);
        _expr += s;
    }

    private void AjouterChiffre(char c)
    {
        Debut(false);
        var m = Regex.Match(_expr, @"[\d.]+$");
        if (m.Success)
        {
            if (m.Value == "0") _expr = _expr[..^1];
            else if (m.Length >= 15) return;
        }
        _expr += c;
    }

    private void AjouterVirgule()
    {
        Debut(false);
        var m = Regex.Match(_expr, @"[\d.]+$");
        if (m.Success)
        {
            if (m.Value.Contains('.')) return;
            _expr += ".";
        }
        else _expr += "0.";
    }

    private void AjouterOperateur(string op)
    {
        Debut(true);
        if (_expr.Length == 0)
        {
            _expr = op == "-" ? "-" : "0" + op;
            return;
        }
        char dernier = _expr[^1];
        if (dernier == '(')
        {
            if (op == "-") _expr += op;
            return;
        }
        if (Operateurs.Contains(dernier))
        {
            if (_expr == "-") return;
            _expr = _expr[..^1] + op;
        }
        else _expr += op;
    }

    private void AjouterPostfixe(string s)
    {
        Debut(true);
        if (_expr.Length == 0) return;
        char dernier = _expr[^1];
        if (Operateurs.Contains(dernier) || dernier == '(') return;
        _expr += s;
    }

    private void FermerParenthese()
    {
        Debut(true);
        if (_expr.Length == 0) return;
        char dernier = _expr[^1];
        if (Operateurs.Contains(dernier) || dernier == '(') return;
        if (_expr.Count(c => c == '(') > _expr.Count(c => c == ')')) _expr += ")";
    }

    private void Effacer()
    {
        _apresEgal = false;
        if (_expr.Length == 0) return;
        foreach (var f in Fonctions)
        {
            if (_expr.EndsWith(f)) { _expr = _expr[..^f.Length]; return; }
        }
        _expr = _expr[..^1];
    }

    private void Reinitialiser()
    {
        _expr = "";
        _resultat = "0";
        _apresEgal = false;
        _erreur = false;
        LblResultat.FontSize = TailleNormale;
        MettreAJour();
    }

    // Calcul
    private static string Completer(string expr)
    {
        int manque = expr.Count(c => c == '(') - expr.Count(c => c == ')');
        return manque > 0 ? expr + new string(')', manque) : expr;
    }

    private void Egal()
    {
        if (_expr.Length == 0 || _apresEgal) return;
        string aEvaluer = Completer(_expr);

        try
        {
            double v = new Evaluateur(aEvaluer, _degres).Calculer();
            _resultat = FormaterNombre(v);
            _expr = aEvaluer;
            _apresEgal = true;

            LblOperation.Text = Affiche(aEvaluer) + " =";
            LblResultat.FontSize = TailleNormale;
            LblResultat.Text = Affiche(_resultat);
            DefilerAFin();
        }
        catch (DivideByZeroException) { AfficherErreur(); }
        catch (ArithmeticException) { AfficherErreur(); }
        catch (FormatException) { AfficherErreur(); }
    }

    private void AfficherErreur()
    {
        _erreur = true;
        _apresEgal = false;
        LblOperation.Text = Affiche(_expr);
        LblResultat.FontSize = 20;
        LblResultat.Text = "Erreur";
    }

    // Affichage
    private void MettreAJour()
    {
        if (_erreur) return;
        LblResultat.FontSize = TailleNormale;

        if (_expr.Length == 0)
        {
            LblOperation.Text = "";
            LblResultat.Text = "0";
        }
        else if (_apresEgal)
        {
            LblOperation.Text = Affiche(_expr) + " =";
            LblResultat.Text = Affiche(_resultat);
        }
        else if (Regex.IsMatch(_expr, @"^[\d.]+$"))
        {
            LblOperation.Text = "";
            LblResultat.Text = Affiche(_expr);
        }
        else
        {
            LblOperation.Text = Affiche(_expr);
            try
            {
                double v = new Evaluateur(Completer(_expr), _degres).Calculer();
                LblResultat.Text = Affiche(FormaterNombre(v));
            }
            catch (DivideByZeroException)
            {
                LblResultat.Text = "Erreur";
            }
            catch { }
        }
        DefilerAFin();
    }

    private void DefilerAFin()
    {
        Dispatcher.Dispatch(async () =>
        {
            try
            {
                await ScrollOperation.ScrollToAsync(ScrollOperation.ContentSize.Width, 0, false);
                await ScrollResultat.ScrollToAsync(ScrollResultat.ContentSize.Width, 0, false);
            }
            catch { }
        });
    }

    private static string Affiche(string s) => s.Replace('.', ',');

    private static string FormaterNombre(double v)
    {
        v = Math.Round(v, 10);
        if (v == 0) return "0";
        if (Math.Abs(v) >= 1e15) return v.ToString("0.#####E+00", CultureInfo.InvariantCulture);
        return v.ToString("0.##########", CultureInfo.InvariantCulture);
    }

    // Évaluateur d'expressions
    private sealed class Evaluateur
    {
        private readonly string _s;
        private readonly bool _deg;
        private int _i;

        public Evaluateur(string s, bool deg)
        {
            _s = s.Replace(" ", "").Replace('/', '÷');
            _deg = deg;
        }

        private char Courant => _i < _s.Length ? _s[_i] : '\0';

        public double Calculer()
        {
            double v = Expression();
            if (_i < _s.Length) throw new FormatException();
            if (double.IsNaN(v) || double.IsInfinity(v)) throw new ArithmeticException();
            return v;
        }

        private double Expression()
        {
            double v = Terme();
            while (Courant == '+' || Courant == '-')
            {
                char op = _s[_i++];
                double d = Terme();
                v = op == '+' ? v + d : v - d;
            }
            return v;
        }

        private double Terme()
        {
            double v = Unaire();
            while (true)
            {
                char c = Courant;
                if (c == '×') { _i++; v *= Unaire(); }
                else if (c == '÷')
                {
                    _i++;
                    double d = Unaire();
                    if (d == 0) throw new DivideByZeroException();
                    v /= d;
                }
                else if (char.IsLetterOrDigit(c) || c == '.' || c == '(' || c == '√')
                    v *= Unaire();
                else return v;
            }
        }

        private double Unaire()
        {
            if (Courant == '-') { _i++; return -Unaire(); }
            if (Courant == '+') { _i++; return Unaire(); }
            return Puissance();
        }

        private double Puissance()
        {
            double b = Postfixe();
            if (Courant == '^')
            {
                _i++;
                double e = Unaire();
                if (b == 0 && e < 0) throw new DivideByZeroException();
                return Math.Pow(b, e);
            }
            return b;
        }

        private double Postfixe()
        {
            double v = Primaire();
            while (true)
            {
                if (Courant == '!') { _i++; v = Factorielle(v); }
                else if (Courant == '%') { _i++; v /= 100; }
                else return v;
            }
        }

        private double Primaire()
        {
            char c = Courant;
            if (char.IsDigit(c) || c == '.') return Nombre();
            if (c == '(') return Argument();
            if (c == 'π') { _i++; return Math.PI; }
            if (c == '√')
            {
                _i++;
                double x = Argument();
                if (x < 0) throw new ArithmeticException();
                return Math.Sqrt(x);
            }
            foreach (var nom in new[] { "sin", "cos", "tan", "log", "ln" })
            {
                if (string.CompareOrdinal(_s, _i, nom, 0, nom.Length) == 0)
                {
                    _i += nom.Length;
                    return Fonction(nom, Argument());
                }
            }
            if (c == 'e') { _i++; return Math.E; }
            throw new FormatException();
        }

        private double Argument()
        {
            if (Courant != '(') throw new FormatException();
            _i++;
            double v = Expression();
            if (Courant != ')') throw new FormatException();
            _i++;
            return v;
        }

        private double Fonction(string nom, double x)
        {
            double rad = _deg ? x * Math.PI / 180 : x;
            switch (nom)
            {
                case "sin": return Nettoyer(Math.Sin(rad));
                case "cos": return Nettoyer(Math.Cos(rad));
                case "tan":
                    if (Math.Abs(Math.Cos(rad)) < 1e-12) throw new ArithmeticException();
                    return Nettoyer(Math.Tan(rad));
                case "ln":
                    if (x <= 0) throw new ArithmeticException();
                    return Math.Log(x);
                case "log":
                    if (x <= 0) throw new ArithmeticException();
                    return Math.Log10(x);
            }
            throw new FormatException();
        }

        private static double Nettoyer(double v) => Math.Abs(v) < 1e-12 ? 0 : v;

        private static double Factorielle(double x)
        {
            if (x < 0 || x != Math.Floor(x) || x > 170) throw new ArithmeticException();
            double r = 1;
            for (int k = 2; k <= (int)x; k++) r *= k;
            return r;
        }

        private double Nombre()
        {
            int debut = _i;
            while (char.IsDigit(Courant) || Courant == '.') _i++;
            if (Courant == 'E' && _i + 1 < _s.Length &&
                (char.IsDigit(_s[_i + 1]) ||
                 ((_s[_i + 1] == '+' || _s[_i + 1] == '-') && _i + 2 < _s.Length && char.IsDigit(_s[_i + 2]))))
            {
                _i += 2;
                while (char.IsDigit(Courant)) _i++;
            }
            return double.Parse(_s[debut.._i], CultureInfo.InvariantCulture);
        }
    }
}