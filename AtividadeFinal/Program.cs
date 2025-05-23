using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using Tao.FreeGlut;
using Tao.OpenGl;

class Program
{
    static bool girarPas = true;
    static float anguloPas = 0f;
    static float anguloLuz = 0;

    static float cameraAngle = 0;
    static float cameraHeight = 5f;
    static float cameraDistance = 10f;

    static bool modoNoite = false;
    static float[] corCeuDia = { 0.5f, 0.8f, 0.92f, 1.0f };
    static float[] corCeuNoite = { 0.05f, 0.05f, 0.15f, 1.0f };

    static uint texturaGrama;

    static List<ParticulaVento> particulas = new List<ParticulaVento>();
    static Random rand = new Random();

    static List<(float x, float z)> posicoesTorres = new List<(float x, float z)>();

    static void Main(string[] args)
    {
        Glut.glutInit();
        Glut.glutInitDisplayMode(Glut.GLUT_DOUBLE | Glut.GLUT_RGB | Glut.GLUT_DEPTH);
        Glut.glutInitWindowSize(1366, 768);
        Glut.glutCreateWindow("Projeto Final");
        Inicializa();
        GerarPosicoesTorres(5, 10);
        InicializarParticulas(1000);
        Glut.glutDisplayFunc(DesenharCena);
        Glut.glutSpecialFunc(TecladoEspecial);
        Glut.glutKeyboardFunc(TecladoNormal);
        Glut.glutIdleFunc(AtualizarCena);
        Glut.glutMainLoop();
    }

    static void DesenharCena()
    {
        Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
        Gl.glLoadIdentity();

        float camX = (float)(cameraDistance * Math.Sin(cameraAngle * Math.PI / 180));
        float camZ = (float)(cameraDistance * Math.Cos(cameraAngle * Math.PI / 180));
        Glu.gluLookAt(camX, cameraHeight, camZ, 0, 0, 0, 0, 1, 0);

        float luzX = 10f * (float)Math.Cos(anguloLuz * Math.PI / 180);
        float luzY = 8f;
        float luzZ = 10f * (float)Math.Sin(anguloLuz * Math.PI / 180);

        float[] lightPos = { luzX, luzY, luzZ, 1.0f };
        Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, lightPos);

        DesenharChao();
        DesenharTorresComPas();
        DesenharSol();
        DesenharParticulasVento();

        Glut.glutSwapBuffers();
    }

    static void DesenharTorresComPas()
    {
        foreach (var pos in posicoesTorres)
        {
            Gl.glPushMatrix();
            Gl.glTranslatef(pos.x, 0, pos.z);

            // Torre
            Gl.glPushMatrix();
            Gl.glTranslatef(0.0f, 1.7f, 0.0f);
            Gl.glScalef(0.3f, 3.0f, 0.3f);
            Gl.glColor3f(0.6f, 0.6f, 0.6f);
            DesenharCubo();
            Gl.glPopMatrix();

            // Pás
            Gl.glPushMatrix();
            Gl.glTranslatef(-0.2f, 3.0f, 0.0f);
            Gl.glRotatef(-90, 0, 0, 1);
            Gl.glRotatef(anguloPas, 0, 1, 0);
            Gl.glColor3f(0.55f, 0.27f, 0.07f);

            Gl.glPushMatrix();
            Gl.glColor3f(0.8f, 0.8f, 0.8f);
            Glut.glutSolidSphere(0.16f, 20, 20);
            Gl.glPopMatrix();

            for (int i = 0; i < 3; i++)
            {
                Gl.glPushMatrix();
                Gl.glRotatef(i * 120, 0, 1, 0);
                Gl.glTranslatef(0.8f, 0.0f, 0.0f);
                Gl.glScalef(1.5f, 0.1f, 0.2f);
                DesenharCubo();
                Gl.glPopMatrix();
            }

            Gl.glPopMatrix();

            Gl.glPopMatrix();
        }
    }

    static void Inicializa()
    {
        Gl.glEnable(Gl.GL_TEXTURE_2D);
        CarregarTextura();
        Gl.glEnable(Gl.GL_DEPTH_TEST);
        Gl.glEnable(Gl.GL_LIGHTING);
        Gl.glEnable(Gl.GL_LIGHT0);
        Gl.glEnable(Gl.GL_COLOR_MATERIAL);

        float[] lightAmbient = { 0.2f, 0.2f, 0.2f, 1.0f };
        float[] lightDiffuse = { 0.8f, 0.8f, 0.8f, 1.0f };
        float[] lightSpecular = { 1.0f, 1.0f, 1.0f, 1.0f };
        float[] lightPosition = { 5.0f, 10.0f, 5.0f, 1.0f };

        Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_AMBIENT, lightAmbient);
        Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_DIFFUSE, lightDiffuse);
        Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_SPECULAR, lightSpecular);
        Gl.glLightfv(Gl.GL_LIGHT0, Gl.GL_POSITION, lightPosition);

        float[] mat_specular = { 1.0f, 1.0f, 1.0f, 1.0f };
        Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_SPECULAR, mat_specular);
        Gl.glMaterialf(Gl.GL_FRONT, Gl.GL_SHININESS, 50.0f);

        Gl.glClearColor(corCeuDia[0], corCeuDia[1], corCeuDia[2], corCeuDia[3]);

        Gl.glMatrixMode(Gl.GL_PROJECTION);
        Gl.glLoadIdentity();
        Glu.gluPerspective(45, 800.0 / 600.0, 1.0, 100.0);
        Gl.glMatrixMode(Gl.GL_MODELVIEW);
    }

    static void CarregarTextura()
    {
        Bitmap bmp = new Bitmap("grama-mine.png");
        bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

        BitmapData data = bmp.LockBits(
            new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        Gl.glGenTextures(1, out texturaGrama);
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, texturaGrama);

        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
        Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);

        Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, data.Width, data.Height,
            0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, data.Scan0);

        bmp.UnlockBits(data);
        bmp.Dispose();
    }

    static void InicializarParticulas(int quantidade)
    {
        particulas.Clear();

        for (int i = 0; i < quantidade; i++)
        {
            float x = (float)(rand.NextDouble() * 40 - 20);
            float y = (float)(1 + rand.NextDouble() * 3);
            float z = (float)(rand.NextDouble() * 40 - 20);

            float velX = 0.1f;
            float velZ = 0.0f;

            particulas.Add(new ParticulaVento(x, y, z, velX, velZ));
        }
    }

    static void GerarPosicoesTorres(int quantidade, float limite = 20)
    {
        posicoesTorres.Clear();

        for (int i = 0; i < quantidade; i++)
        {
            float x = (float)(rand.NextDouble() * 2 * limite - limite);
            float z = (float)(rand.NextDouble() * 2 * limite - limite);

            posicoesTorres.Add((x, z));
        }
    }

    static void DesenharSol()
    {
        float raio = 10f;
        float altura = 8f;
        float x = raio * (float)Math.Cos(anguloLuz * Math.PI / 180);
        float z = raio * (float)Math.Sin(anguloLuz * Math.PI / 180);
        float y = altura;

        Gl.glPushMatrix();
        Gl.glTranslatef(x, y, z);

        float[] emissiveDay = { 1.0f, 1.0f, 0.0f, 1.0f };
        float[] emissiveNight = { 1.0f, 1.0f, 1.0f, 1.0f };

        if (modoNoite)
        {
            Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_EMISSION, emissiveNight);
            Gl.glColor3f(1.0f, 1.0f, 1.0f);
        }
        else
        {
            Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_EMISSION, emissiveDay);
            Gl.glColor3f(1.0f, 1.0f, 0.0f);
        }

        Glut.glutSolidSphere(0.8f, 20, 20);

        float[] noEmission = { 0f, 0f, 0f, 1.0f };
        Gl.glMaterialfv(Gl.GL_FRONT, Gl.GL_EMISSION, noEmission);

        Gl.glPopMatrix();
    }


    static void TecladoNormal(byte tecla, int x, int y)
    {
        if (tecla == 'n' || tecla == 'N')
            AlternarModoDiaNoite();

        if (tecla == 'p' || tecla == 'P')
        {
            girarPas = !girarPas;
        }
    }
    static void AlternarModoDiaNoite()
    {
        modoNoite = !modoNoite;

        if (modoNoite)
            Gl.glClearColor(corCeuNoite[0], corCeuNoite[1], corCeuNoite[2], corCeuNoite[3]);
        else
            Gl.glClearColor(corCeuDia[0], corCeuDia[1], corCeuDia[2], corCeuDia[3]);

        Glut.glutPostRedisplay();
    }


    static void TecladoEspecial(int tecla, int x, int y)
    {
        if (tecla == Glut.GLUT_KEY_LEFT)
            cameraAngle -= 5f;
        else if (tecla == Glut.GLUT_KEY_RIGHT)
            cameraAngle += 5f;
        else if (tecla == Glut.GLUT_KEY_UP)
            cameraHeight += 0.5f;
        else if (tecla == Glut.GLUT_KEY_DOWN)
            cameraHeight -= 0.5f;
        else if (tecla == Glut.GLUT_KEY_PAGE_UP)
            cameraDistance += 1f;
        else if (tecla == Glut.GLUT_KEY_PAGE_DOWN)
            cameraDistance -= 1f;

        if (cameraDistance < 3f) cameraDistance = 3f;
        if (cameraDistance > 50f) cameraDistance = 50f;


        if (cameraAngle >= 360f) cameraAngle -= 360f;
        if (cameraAngle < 0f) cameraAngle += 360f;

        if (cameraHeight > 20f) cameraHeight = 20f;
        if (cameraHeight < 1f) cameraHeight = 1f;

        Glut.glutPostRedisplay();
    }

    static void DesenharParticulasVento()
    {
        Gl.glDisable(Gl.GL_LIGHTING);
        Gl.glColor3f(0.7f, 0.8f, 1.0f);

        Gl.glBegin(Gl.GL_LINES);
        foreach (var p in particulas)
        {
            Gl.glVertex3f(p.x, p.y, p.z);
            Gl.glVertex3f(p.x + p.velocidadeX * 3, p.y, p.z + p.velocidadeZ * 3);
        }
        Gl.glEnd();

        Gl.glEnable(Gl.GL_LIGHTING);
    }

    static void AtualizarCena()
    {
        cameraAngle += 0.2f;
        if (cameraAngle >= 360f)
        {
            cameraAngle -= 360f;
            AlternarModoDiaNoite();
        }

        anguloLuz += 0.5f;
        if (anguloLuz >= 360f)
            anguloLuz -= 360f;

        if (girarPas)
        {
            anguloPas += 1f;
            if (anguloPas >= 360f)
                anguloPas -= 360f;

            foreach (var p in particulas)
            {
                p.Atualizar();
            }

            if (rand.NextDouble() < 0.2)
            {
                particulas.Add(new ParticulaVento(
                    (float)(rand.NextDouble() * 40 - 20),
                    (float)(1 + rand.NextDouble() * 3),
                    (float)(rand.NextDouble() * 40 - 20),
                    0.1f, 0.0f));
            }

            particulas.RemoveAll(p => !p.ativa);
        }
        else
        {
            particulas.Clear();
        }

        Glut.glutPostRedisplay();
    }


    static void DesenharChao()
    {
        Gl.glEnable(Gl.GL_TEXTURE_2D);
        Gl.glBindTexture(Gl.GL_TEXTURE_2D, texturaGrama);

        Gl.glBegin(Gl.GL_QUADS);
        Gl.glNormal3f(0.0f, 1.0f, 0.0f);

        Gl.glTexCoord2f(0, 0); Gl.glVertex3f(-20.0f, 0.0f, -20.0f);
        Gl.glTexCoord2f(5, 0); Gl.glVertex3f(20.0f, 0.0f, -20.0f);
        Gl.glTexCoord2f(5, 5); Gl.glVertex3f(20.0f, 0.0f, 20.0f);
        Gl.glTexCoord2f(0, 5); Gl.glVertex3f(-20.0f, 0.0f, 20.0f);
        Gl.glEnd();

        Gl.glDisable(Gl.GL_TEXTURE_2D);
    }


    static void DesenharCubo()
    {
        Gl.glColor3f(0.6f, 0.6f, 0.6f);
        Glut.glutSolidCube(1.0f);
    }
}

class ParticulaVento
{
    public float x, y, z;
    public float velocidadeX, velocidadeZ;
    public bool ativa;

    public ParticulaVento(float x, float y, float z, float velX, float velZ)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.velocidadeX = velX;
        this.velocidadeZ = velZ;
        this.ativa = true;
    }

    public void Atualizar()
    {
        x += velocidadeX;
        z += velocidadeZ;

        if (x > 20) x = -20;
        if (x < -20) x = 20;
        if (z > 20) z = -20;
        if (z < -20) z = 20;
    }
}